using System.Collections;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Runtime.LevelCreation;
using UnityEngine;
using DG.Tweening;
using TMPro;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Data;

public class FlowerRouletteManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rotatingRoot;
    [SerializeField] private Transform shooterEnterPoint;
    [SerializeField] private List<FlowerPetal> petals = new List<FlowerPetal>();
    [SerializeField] private MiddleSlotManager middleSlotManager;
    [SerializeField] private BottomSlotManager bottomSlotManager;
    [SerializeField] private GameManager gameManager;

    [Header("UI")]
    [SerializeField] private TMP_Text petalCountText;

    [Header("No Petal Feedback")]
    [SerializeField] private Color noPetalTextColor = Color.red;
    [SerializeField] private float noPetalSingleShakeDuration = 0.02f;
    [SerializeField] private int noPetalShakeLoopCount = 3;
    [SerializeField] private float noPetalReturnColorDuration = 0.08f;
    [SerializeField] private float noPetalShakeZDistance = 0.18f;
    [SerializeField] private Ease noPetalShakeEase = Ease.Linear;

    private Color petalCountDefaultColor;
    private Vector3 petalCountDefaultLocalPosition;
    private Sequence noPetalFeedbackSequence;
    private bool petalCountDefaultsCached;

    [Header("Settings")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float roundDuration = 4f;
    [Header("End Game Flower Hold")]
    [SerializeField] private bool keepShootersOnFlowerWhenFewLeft = true;
    //[SerializeField] private int keepOnFlowerShooterThreshold = 5;
    [SerializeField] private float endGameRotateSpeedMultiplier = 2f;

    [Header("Shooter Move Animation")]
    [SerializeField] private float shooterJumpPower = 3f;
    [SerializeField] private int shooterJumpCount = 1;
    [SerializeField] private float shooterFlipAngle = 360f;

    [Header("Shooter Look Settings")]
    [SerializeField] private Vector3 shooterLookEulerOffset = Vector3.zero;
    [Header("Shooter Rotation Animation")]
    [SerializeField] private Ease shooterRotateEase = Ease.OutQuad;

    [Header("Audio")]
    [AudioClipName] public string petalAttachSound;

    [Header("Return To Middle")]
    [SerializeField] private float returnToMiddleStartDelay = 0.08f;

    [Header("Flower Entry Safety")]
    [SerializeField] private bool waitForEnterPointClear = true;
    [SerializeField] private float enterPointBlockRadius = 1.25f;
    [SerializeField] private float maxEnterPointWaitTime = 2f;

    private readonly Dictionary<Shooter, FlowerPetal> shooterPetalMap = new Dictionary<Shooter, FlowerPetal>();
    private readonly HashSet<Shooter> linkedRoundCompletedShooters = new HashSet<Shooter>();
    private void Update()
    {
        RotateFlower();
    }
    public void ConfigureRuntimeReferences(
    MiddleSlotManager runtimeMiddleSlotManager,
    BottomSlotManager runtimeBottomSlotManager,
    GameManager runtimeGameManager)
    {
        middleSlotManager = runtimeMiddleSlotManager;
        bottomSlotManager = runtimeBottomSlotManager;
        gameManager = runtimeGameManager;
    }
    public void Setup(LevelContainer levelContainer)
    {
        petals.Clear();

        if (levelContainer != null)
        {
            if (rotatingRoot == null && levelContainer.FlowerParent != null)
            {
                rotatingRoot = levelContainer.FlowerParent.transform;
            }

            foreach (GameObject petalObject in levelContainer.GeneratedFlowerPetals)
            {
                if (petalObject == null)
                    continue;

                GeneratedLevelItem generatedItem = petalObject.GetComponent<GeneratedLevelItem>();

                if (generatedItem == null)
                    continue;

                if (generatedItem.itemType != GeneratedLevelItemType.FlowerPetal)
                    continue;

                FlowerPetal petal = petalObject.GetComponent<FlowerPetal>();

                if (petal == null)
                {
                    petal = petalObject.AddComponent<FlowerPetal>();
                }

                petal.Setup(generatedItem.orderIndex);
                petals.Add(petal);
            }
        }

        SetupPetals();

        Debug.Log($"[FlowerRouletteManager] Petal count: {petals.Count}");

        CachePetalCountDefaults();
        UpdatePetalCountText();


        if (petals.Count == 0)
        {
            Debug.LogError("[FlowerRouletteManager] Petal listesi boþ. Generated flower petal objelerinde FlowerPetal component var mý?");
        }

        if (rotatingRoot == null)
        {
            Debug.LogError("[FlowerRouletteManager] RotatingRoot null.");
        }

        if (shooterEnterPoint == null)
        {
            Debug.LogError("[FlowerRouletteManager] ShooterEnterPoint null.");
        }
    }

    private void SetupPetals()
    {
        for (int i = 0; i < petals.Count; i++)
        {
            if (petals[i] != null)
            {
                petals[i].Setup(i);
            }
        }
    }
    private void RotateFlower()
    {
        if (rotatingRoot == null)
            return;

        float currentRotateSpeed = rotateSpeed;

        if (ShouldUseEndGameFlowerMode())
        {
            currentRotateSpeed *= endGameRotateSpeedMultiplier;
        }

        rotatingRoot.Rotate(Vector3.up, currentRotateSpeed * Time.deltaTime);
    }

    public IEnumerator SendShootersToFlowerRoutine(ShooterTransferRequest request)
    {
        if (request == null || request.Shooters == null)
            yield break;

        if (!HasEnoughEmptyPetals(request.Shooters.Count))
        {
            PlayNoPetalFeedback();

            foreach (Shooter shooter in request.Shooters)
            {
                if (shooter != null)
                {
                    shooter.UnlockTransfer();
                }
            }

            yield break;
        }

        List<FlowerPetal> reservedPetals = ReservePetals(request.Shooters.Count);

        for (int i = 0; i < request.Shooters.Count; i++)
        {
            Shooter shooter = request.Shooters[i];
            FlowerPetal petal = reservedPetals[i];

            if (shooter == null || petal == null)
                continue;

            yield return MoveShooterToFlowerRoutine(shooter, petal);

            if (request.IsGroup && i < request.Shooters.Count - 1)
            {
                float delay = ShooterTransferQueue.Instance != null
                    ? ShooterTransferQueue.Instance.DelayBetweenLinkedShooters
                    : 0.5f;

                yield return new WaitForSeconds(delay);
            }
        }
    }
    private IEnumerator MoveShooterToFlowerRoutine(Shooter shooter, FlowerPetal petal)
    {
        if (shooter == null || petal == null)
            yield break;

        if (shooter.IsDestroyed)
            yield break;

        shooter.LockTransfer();

        yield return WaitForEnterPointClear(shooter);

        shooter.SetState(ShooterState.MovingToFlower);

        ClearShooterFromCurrentSlot(shooter);

        Vector3 targetPos = shooterEnterPoint != null
            ? shooterEnterPoint.position
            : rotatingRoot.position;

        petal.AttachToShooterInstant(shooter, shooter.PetalAttachPoint);
        PlayPetalAttachSound();
        UpdatePetalCountText();

        Quaternion targetRotation = GetShooterOutwardRotation(targetPos);

        yield return MoveShooterJumpRoutine(shooter, targetPos, targetRotation);

        if (rotatingRoot != null)
        {
            shooter.transform.SetParent(rotatingRoot, true);
        }

        shooterPetalMap[shooter] = petal;

        shooter.OnShooterDestroyed += HandleShooterDestroyedOnFlower;

        shooter.StartFlowerRound();

        StartCoroutine(FlowerRoundRoutine(shooter));
    }
    private void PlayPetalAttachSound()
    {
        if (AudioManager.instance == null)
            return;

        AudioManager.instance.PlaySound(petalAttachSound);
    }

    private IEnumerator MoveShooterJumpRoutine(
        Shooter shooter,
        Vector3 targetPosition,
        Quaternion targetRotation)
    {
        if (shooter == null)
            yield break;

        Transform shooterTransform = shooter.transform;

        shooter.ForceResetScaleAnimations(false);

        shooterTransform.DOKill(false);
        shooter.PlayJumpArmAnimation(moveDuration);

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            shooterTransform
                .DOJump(targetPosition, shooterJumpPower, shooterJumpCount, moveDuration)
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            shooterTransform
                .DORotateQuaternion(targetRotation, moveDuration)
                .SetEase(shooterRotateEase)
        );

        bool completed = false;

        sequence.OnComplete(() =>
        {
            shooterTransform.position = targetPosition;
            shooterTransform.rotation = targetRotation;
            shooter.ResetArmPose();
            shooter.ForceResetScaleAnimations(true);
            completed = true;
        });

        yield return new WaitUntil(() => completed);
    }

    private IEnumerator FlowerRoundRoutine(Shooter shooter)
    {
        float timer = 0f;

        float accumulatedAngle = 0f;
        float previousYAngle = rotatingRoot != null
            ? rotatingRoot.eulerAngles.y
            : 0f;

        while (timer < roundDuration)
        {
            if (shooter == null || shooter.IsDestroyed)
                yield break;

            float deltaTime = Time.deltaTime;
            timer += deltaTime;

            if (rotatingRoot != null &&
                shooter.State == ShooterState.OnFlower &&
                shooter.HasBullet)
            {
                float currentYAngle = rotatingRoot.eulerAngles.y;
                float deltaAngle = Mathf.Abs(Mathf.DeltaAngle(previousYAngle, currentYAngle));

                previousYAngle = currentYAngle;
                accumulatedAngle += deltaAngle;

                if (accumulatedAngle >= 360f)
                {
                    int completedLapCount = Mathf.FloorToInt(accumulatedAngle / 360f);
                    accumulatedAngle -= completedLapCount * 360f;

                    shooter.ResetHitColumnsForNewLap();
                }
            }

            yield return null;
        }

        CompleteShooterRound(shooter);
    }

    private void CompleteShooterRound(Shooter shooter)
    {
        if (shooter == null || shooter.IsDestroyed)
            return;

        if (!shooter.HasBullet)
        {
            shooter.OnShooterDestroyed -= HandleShooterDestroyedOnFlower;
            shooter.EndFlowerRound();
            ReleasePetalOfShooter(shooter);
            shooter.transform.SetParent(null, true);
            shooter.DestroyShooter();
            return;
        }

        if (shooter.LinkGroup != null)
        {
            HandleLinkedShooterRoundComplete(shooter);
            return;
        }

        if (ShouldKeepShooterOnFlower(shooter))
        {
            Debug.Log(
                $"[FlowerRouletteManager] Shooter flower üzerinde kalacak ve yeni round baþlayacak. " +
                $"AliveShooterCount:{GetAliveSceneShooterCount()}"
            );

            RestartFlowerRoundWithoutLeaving(shooter);
            return;
        }

        StartCoroutine(ReturnShooterToMiddleRoutine(shooter));
    }

    private IEnumerator ReturnShooterToMiddleRoutine(Shooter shooter)
    {
        if (shooter == null || shooter.IsDestroyed)
            yield break;

        shooter.OnShooterDestroyed -= HandleShooterDestroyedOnFlower;

        shooter.EndFlowerRound();
        shooter.SetState(ShooterState.ReturningToMiddle);

        if (returnToMiddleStartDelay > 0f)
        {
            yield return new WaitForSeconds(returnToMiddleStartDelay);
        }

        ReleasePetalOfShooter(shooter);

        shooter.transform.SetParent(null, true);

        if (middleSlotManager == null)
        {
            middleSlotManager = FindFirstObjectByType<MiddleSlotManager>();
        }

        bool placed = false;

        if (middleSlotManager != null)
        {
            yield return middleSlotManager.TryPlaceShooterRoutine(
                shooter,
                0f,
                result => placed = result
            );
        }

        if (!placed && gameManager != null)
        {
            gameManager.GameLose();
        }
    }

    private void HandleShooterDestroyedOnFlower(Shooter shooter)
    {
        if (shooter == null)
            return;

        linkedRoundCompletedShooters.Remove(shooter);

        shooter.OnShooterDestroyed -= HandleShooterDestroyedOnFlower;
        ReleasePetalOfShooter(shooter);
    }

    private void ClearShooterFromCurrentSlot(Shooter shooter)
    {
        if (shooter == null)
            return;

        if (shooter.CurrentBottomNode != null)
        {
            if (bottomSlotManager == null)
            {
                bottomSlotManager = FindFirstObjectByType<BottomSlotManager>();
            }

            if (bottomSlotManager != null)
            {
                bottomSlotManager.NotifyShooterRemoved(shooter);
            }
            else
            {
                Debug.LogError(
                    "[FlowerRouletteManager] BottomSlotManager null. " +
                    "Bottom slot refill çalýþmayacak. Manager referansýný runtime'da baðla."
                );
            }

            return;
        }

        //if (shooter.CurrentMiddleNode != null)
        //{
        //    shooter.CurrentMiddleNode.ClearShooter();
        //    shooter.ClearPlacement();
        //}

        if (shooter.CurrentMiddleNode != null)
        {
            if (middleSlotManager == null)
            {
                middleSlotManager = FindFirstObjectByType<MiddleSlotManager>();
            }

            if (middleSlotManager != null)
            {
                middleSlotManager.RemoveShooter(shooter);
            }
            else
            {
                Debug.LogError(
                    "[FlowerRouletteManager] MiddleSlotManager null. " +
                    "Middle slot compact çalýþmayacak."
                );

                shooter.CurrentMiddleNode.ClearShooter();
            }
        }
    }

    private bool HasEnoughEmptyPetals(int neededCount)
    {
        int emptyCount = 0;

        foreach (FlowerPetal petal in petals)
        {
            if (petal != null && petal.IsEmpty)
            {
                emptyCount++;
            }
        }

        return emptyCount >= neededCount;
    }

    private List<FlowerPetal> ReservePetals(int count)
    {
        List<FlowerPetal> result = new List<FlowerPetal>();

        foreach (FlowerPetal petal in petals)
        {
            if (petal != null && petal.IsEmpty)
            {
                result.Add(petal);

                if (result.Count >= count)
                    break;
            }
        }

        return result;
    }

    private void ReleasePetalOfShooter(Shooter shooter)
    {
        if (shooter == null)
            return;

        if (!shooterPetalMap.ContainsKey(shooter))
            return;

        FlowerPetal petal = shooterPetalMap[shooter];

        if (petal != null)
        {
            petal.Release(UpdatePetalCountText);
        }

        shooterPetalMap.Remove(shooter);
        UpdatePetalCountText();

    }

    private Quaternion GetShooterOutwardRotation(Vector3 shooterPosition)
    {
        if (rotatingRoot == null)
            return Quaternion.identity;

        Vector3 direction = shooterPosition - rotatingRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return Quaternion.identity;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return targetRotation * Quaternion.Euler(shooterLookEulerOffset);
    }

    private void UpdatePetalCountText()
    {
        if (petalCountText == null)
            return;

        int totalPetalCount = petals.Count;
        int emptyPetalCount = GetEmptyPetalCount();

        petalCountText.text = $"{totalPetalCount}/{emptyPetalCount}";
    }

    private int GetEmptyPetalCount()
    {
        int emptyCount = 0;

        foreach (FlowerPetal petal in petals)
        {
            if (petal != null && petal.IsEmpty)
            {
                emptyCount++;
            }
        }

        return emptyCount;
    }
    private bool ShouldKeepShooterOnFlower(Shooter shooter)
    {
        if (!keepShootersOnFlowerWhenFewLeft)
            return false;

        if (shooter == null || shooter.IsDestroyed)
            return false;

        if (!shooter.HasBullet)
            return false;

        if (shooter.State != ShooterState.OnFlower)
            return false;

        return ShouldUseEndGameFlowerMode();
    }

    private int GetAliveSceneShooterCount()
    {
        Shooter[] shooters = FindObjectsByType<Shooter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int count = 0;

        foreach (Shooter shooter in shooters)
        {
            if (shooter == null)
                continue;

            if (shooter.IsDestroyed)
                continue;

            count++;
        }

        return count;
    }
    private void RestartFlowerRoundWithoutLeaving(Shooter shooter)
    {
        if (shooter == null || shooter.IsDestroyed)
            return;

        if (!shooter.HasBullet)
            return;

        // Önce bu round'un column hit kayýtlarýný temizle.
        shooter.EndFlowerRound();

        // Shooter hâlâ flower üzerinde kalacak.
        // Petal release edilmeyecek.
        // Parent rotatingRoot altýnda kalacak.
        // State tekrar OnFlower olacak.
        shooter.StartFlowerRound();

        StartCoroutine(FlowerRoundRoutine(shooter));
    }

    private void CachePetalCountDefaults()
    {
        if (petalCountText == null)
            return;

        if (petalCountDefaultsCached)
            return;

        petalCountDefaultColor = petalCountText.color;
        petalCountDefaultLocalPosition = petalCountText.transform.localPosition;
        petalCountDefaultsCached = true;
    }

    private void PlayNoPetalFeedback()
    {
        if (petalCountText == null)
            return;

        CachePetalCountDefaults();

        Transform textTransform = petalCountText.transform;

        if (noPetalFeedbackSequence != null)
        {
            noPetalFeedbackSequence.Kill();
            noPetalFeedbackSequence = null;
        }

        textTransform.DOKill();

        textTransform.localPosition = petalCountDefaultLocalPosition;
        petalCountText.color = noPetalTextColor;

        Vector3 forwardZPosition = petalCountDefaultLocalPosition + new Vector3(0f, 0f, noPetalShakeZDistance);
        Vector3 backwardZPosition = petalCountDefaultLocalPosition - new Vector3(0f, 0f, noPetalShakeZDistance);

        noPetalFeedbackSequence = DOTween.Sequence();

        noPetalFeedbackSequence.Append(
            textTransform
                .DOLocalMove(forwardZPosition, noPetalSingleShakeDuration)
                .SetEase(noPetalShakeEase)
        );

        for (int i = 0; i < noPetalShakeLoopCount; i++)
        {
            noPetalFeedbackSequence.Append(
                textTransform
                    .DOLocalMove(backwardZPosition, noPetalSingleShakeDuration)
                    .SetEase(noPetalShakeEase)
            );

            noPetalFeedbackSequence.Append(
                textTransform
                    .DOLocalMove(forwardZPosition, noPetalSingleShakeDuration)
                    .SetEase(noPetalShakeEase)
            );
        }

        noPetalFeedbackSequence.Append(
            textTransform
                .DOLocalMove(petalCountDefaultLocalPosition, noPetalSingleShakeDuration)
                .SetEase(Ease.OutQuad)
        );

        noPetalFeedbackSequence.Append(
            DOTween.To(
                () => petalCountText.color,
                value => petalCountText.color = value,
                petalCountDefaultColor,
                noPetalReturnColorDuration
            )
        );

        noPetalFeedbackSequence.OnComplete(() =>
        {
            textTransform.localPosition = petalCountDefaultLocalPosition;
            petalCountText.color = petalCountDefaultColor;
            noPetalFeedbackSequence = null;
        });
    }
    private void OnDisable()
    {
        if (noPetalFeedbackSequence != null)
        {
            noPetalFeedbackSequence.Kill();
            noPetalFeedbackSequence = null;
        }

        if (petalCountText != null && petalCountDefaultsCached)
        {
            petalCountText.transform.localPosition = petalCountDefaultLocalPosition;
            petalCountText.color = petalCountDefaultColor;
        }
    }

    private void HandleLinkedShooterRoundComplete(Shooter shooter)
    {
        if (shooter == null || shooter.IsDestroyed)
            return;

        if (shooter.LinkGroup == null)
            return;

        shooter.EndFlowerRound();

        // Artýk ateþ etmesin, ama petal üstünde beklesin.
        shooter.SetState(ShooterState.WaitingLinkedReturn);

        linkedRoundCompletedShooters.Add(shooter);

        if (!IsLinkedGroupReadyToResolve(shooter.LinkGroup))
            return;

        ResolveLinkedGroupAfterRound(shooter.LinkGroup);
    }

    private bool IsLinkedGroupReadyToResolve(ShooterLinkGroup linkGroup)
    {
        if (linkGroup == null)
            return false;

        IReadOnlyList<Shooter> groupShooters = linkGroup.Shooters;

        if (groupShooters == null || groupShooters.Count == 0)
            return false;

        foreach (Shooter shooter in groupShooters)
        {
            if (shooter == null || shooter.IsDestroyed)
                continue;

            // Grup üyesi henüz flower üzerinde deðilse bekle.
            if (!shooterPetalMap.ContainsKey(shooter))
                return false;

            // Grup üyesi round bitirmediyse bekle.
            if (!linkedRoundCompletedShooters.Contains(shooter))
                return false;
        }

        return true;
    }

    private void ResolveLinkedGroupAfterRound(ShooterLinkGroup linkGroup)
    {
        if (linkGroup == null)
            return;

        List<Shooter> activeGroupShooters = new List<Shooter>();

        foreach (Shooter shooter in linkGroup.Shooters)
        {
            if (shooter == null || shooter.IsDestroyed)
                continue;

            if (!shooterPetalMap.ContainsKey(shooter))
                continue;

            activeGroupShooters.Add(shooter);
        }

        if (activeGroupShooters.Count == 0)
            return;

        int aliveShooterCount = GetAliveSceneShooterCount();

        if (keepShootersOnFlowerWhenFewLeft &&
            ShouldUseEndGameFlowerMode())
        {
            RestartLinkedGroupWithoutLeaving(activeGroupShooters);
            return;
        }

        ReturnLinkedGroupToMiddle(activeGroupShooters);
    }

    private void RestartLinkedGroupWithoutLeaving(List<Shooter> groupShooters)
    {
        foreach (Shooter shooter in groupShooters)
        {
            if (shooter == null || shooter.IsDestroyed)
                continue;

            linkedRoundCompletedShooters.Remove(shooter);

            RestartFlowerRoundWithoutLeaving(shooter);
        }
    }

    private void ReturnLinkedGroupToMiddle(List<Shooter> groupShooters)
    {
        StartCoroutine(ReturnLinkedGroupToMiddleRoutine(groupShooters));
    }
    private IEnumerator ReturnLinkedGroupToMiddleRoutine(List<Shooter> groupShooters)
    {
        if (groupShooters == null || groupShooters.Count == 0)
            yield break;

        for (int i = 0; i < groupShooters.Count; i++)
        {
            Shooter shooter = groupShooters[i];

            if (shooter == null || shooter.IsDestroyed)
                continue;

            linkedRoundCompletedShooters.Remove(shooter);

            shooter.OnShooterDestroyed -= HandleShooterDestroyedOnFlower;

            shooter.EndFlowerRound();
            shooter.SetState(ShooterState.ReturningToMiddle);

            ReleasePetalOfShooter(shooter);

            shooter.transform.SetParent(null, true);

            if (middleSlotManager == null)
            {
                middleSlotManager = FindFirstObjectByType<MiddleSlotManager>();
            }

            bool placed = false;

            if (middleSlotManager != null)
            {
                yield return middleSlotManager.TryPlaceShooterRoutine(
                    shooter,
                    0f,
                    result => placed = result
                );
            }

            if (!placed)
            {
                if (gameManager != null)
                {
                    gameManager.GameLose();
                }

                yield break;
            }

            float delay = ShooterTransferQueue.Instance != null
                ? ShooterTransferQueue.Instance.DelayBetweenLinkedShooters
                : 0.15f;

            if (i < groupShooters.Count - 1 && delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private IEnumerator WaitForEnterPointClear(Shooter incomingShooter)
    {
        if (!waitForEnterPointClear)
            yield break;

        if (shooterEnterPoint == null)
            yield break;

        float timer = 0f;

        while (IsEnterPointBlocked(incomingShooter))
        {
            timer += Time.deltaTime;

            if (maxEnterPointWaitTime > 0f && timer >= maxEnterPointWaitTime)
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool IsEnterPointBlocked(Shooter incomingShooter)
    {
        if (shooterEnterPoint == null)
            return false;

        foreach (Shooter flowerShooter in shooterPetalMap.Keys)
        {
            if (flowerShooter == null)
                continue;

            if (flowerShooter == incomingShooter)
                continue;

            if (flowerShooter.IsDestroyed)
                continue;

            float distance = Vector3.Distance(
                flowerShooter.transform.position,
                shooterEnterPoint.position
            );

            if (distance <= enterPointBlockRadius)
            {
                return true;
            }
        }

        return false;
    }
    private bool ShouldUseEndGameFlowerMode()
    {
        Shooter[] allShooters = FindObjectsByType<Shooter>(FindObjectsSortMode.None);

        if (allShooters == null || allShooters.Length == 0)
            return false;

        int aliveShooterCount = 0;

        foreach (Shooter shooter in allShooters)
        {
            if (shooter == null || shooter.IsDestroyed)
                continue;

            aliveShooterCount++;

            if (!IsShooterInFlowerFlow(shooter))
                return false;
        }

        return aliveShooterCount > 0;
    }

    private bool IsShooterInFlowerFlow(Shooter shooter)
    {
        if (shooter == null || shooter.IsDestroyed)
            return false;

        return shooter.State == ShooterState.MovingToFlower ||
               shooter.State == ShooterState.OnFlower ||
               shooter.State == ShooterState.WaitingLinkedReturn;
    }
}