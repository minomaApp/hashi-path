using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using DG.Tweening;
using UnityEngine;
using TMPro;
using System.Collections;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Data;
using BoxPuller.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Utilities;
using TemplateProject.Scripts.Data.SO;
using EPOOutline;

public class Shooter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EnumHolder.GameColor color;
    [SerializeField] private int bulletCount;
    [SerializeField] private int linkGroupId = -1;
    [SerializeField] private int laneIndex = -1;
    [SerializeField] private int orderIndex = -1;
    [SerializeField] private bool isHidden;
    [SerializeField] private bool isRevealed;


    [Header("Runtime")]
    [SerializeField] private ShooterState state;
    [SerializeField] private bool isTransferLocked;

    private BottomSlotNode currentBottomNode;
    private MiddleSlotNode currentMiddleNode;
    private ShooterLinkGroup linkGroup;

    private readonly HashSet<int> hitColumnsThisRound = new HashSet<int>();

    [Header("References")]
    [SerializeField] private Transform petalAttachPoint;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform rayTargetPoint;
    [SerializeField] private Transform mouthTransform;
    [SerializeField] private Transform bulletSpawnPoint;

    [Header("Link Visual")]
    [SerializeField] private GameObject linkLineObject;
    [SerializeField] private LineRenderer linkLineRenderer;
    [SerializeField] private Transform linkLinePoint;
    [Header("Link Hat Visual")]
    [SerializeField] private GameObject hatObject;

    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 50f;
    [SerializeField] private float raycastRadius = 0.25f;
    [SerializeField] private LayerMask boxLayerMask = ~0;

    [Header("Visual")]
    [SerializeField] private Renderer[] mainColorRenderers;
    [SerializeField] private Renderer[] darkColorRenderers;

    [Header("Outline")]
    [SerializeField] private Outlinable outline;

    [Header("Mouth Pull Animation")]
    [SerializeField] private Vector3 mouthPullLocalOffset = new Vector3(0f, 0f, -0.12f);
    [SerializeField] private float mouthPullDuration = 0.08f;
    [SerializeField] private Ease mouthPullEase = Ease.OutQuad;

    [Header("Bullet Count UI")]
    [SerializeField] private TMP_Text bulletCountText;
    [SerializeField, Range(0, 255)] private int selectableTextAlpha = 255;
    [SerializeField, Range(0, 255)] private int lockedTextAlpha = 146;

    [Header("Arm Jump Animation")]
    [SerializeField] private Transform leftArmTransform;
    [SerializeField] private Transform rightArmTransform;

    [SerializeField] private float armOpenXAngle = 35f;
    [SerializeField] private float armOpenDurationRatio = 0.45f;
    [SerializeField] private Ease armOpenEase = Ease.OutQuad;
    [SerializeField] private Ease armCloseEase = Ease.InQuad;


    [Header("Selectable Breathing Animation")]
    [SerializeField] private Transform breathingRoot;
    [SerializeField] private Vector3 breathingScaleMultiplier = new Vector3(1.025f, 1.035f, 1.025f);
    [SerializeField] private float breathingDuration = 0.75f;
    [SerializeField] private Ease breathingEase = Ease.InOutSine;

    [Header("Scale Safety")]
    [Tooltip("Açýkken shooter scale animasyonlarý her zaman bu sabit scale deðerini default kabul eder. Büyümüþ scale yanlýþlýkla default olmaz.")]
    [SerializeField] private bool useFixedDefaultScale = true;
    [SerializeField] private Vector3 fixedDefaultScale = Vector3.one;

    [Header("Bottom Idle Look Animation")]
    [SerializeField] private bool enableBottomIdleLookAnimation = true;
    [SerializeField] private Vector2 bottomIdleLookDelayRange = new Vector2(5f, 10f);

    [SerializeField] private float bottomIdleLookJumpPower = 0.45f;
    [SerializeField] private int bottomIdleLookJumpCount = 1;
    [SerializeField] private float bottomIdleLookTurnDuration = 0.28f;
    [SerializeField] private float bottomIdleLookBackHoldDuration = 0.45f;

    [SerializeField] private Ease bottomIdleLookJumpEase = Ease.OutQuad;
    [SerializeField] private Ease bottomIdleLookRotateEase = Ease.OutQuad;


    [Header("Shoot Reaction Animation")]
    [SerializeField] private Transform shootScaleRoot;
    [SerializeField] private Vector3 shootScaleMultiplier = new Vector3(1.08f, 1.08f, 1.08f);
    [SerializeField] private float shootArmXAngle = 35f;
    [SerializeField] private Ease shootScaleInEase = Ease.OutBack;
    [SerializeField] private Ease shootScaleOutEase = Ease.OutQuad;

    [Header("Locked Click Feedback Animation")]
    [SerializeField] private Vector3 lockedClickScaleMultiplier = new Vector3(1.12f, 1.12f, 1.12f);
    [SerializeField] private float lockedClickScaleDuration = 0.08f;
    [SerializeField] private Ease lockedClickScaleInEase = Ease.OutBack;
    [SerializeField] private Ease lockedClickScaleOutEase = Ease.OutQuad;

    [Header("Locked Click Feedback Audio/Haptic")]
    [AudioClipName] public string lockedClickSound;
    [SerializeField] private bool useLockedClickVibration = true;

    private Sequence lockedClickSequence;

    private Vector3 shootScaleDefaultLocalScale;
    private Sequence shootReactionSequence;

    private Shooter linkVisualTarget;

    private Vector3 mouthDefaultLocalPosition;
    private Vector3 breathingDefaultLocalScale;
    private Tween breathingTween;

    private Quaternion leftArmDefaultLocalRotation;
    private Quaternion rightArmDefaultLocalRotation;
    private bool animationDefaultsCached;
    private Coroutine bottomIdleLookCoroutine;
    private Sequence bottomIdleLookSequence;
    private bool isBottomIdleLookPlaying;
    private GameColors cachedGameColors;

    public event Action<Shooter> OnShooterDestroyed;

    public Transform PetalAttachPoint => petalAttachPoint;

    public EnumHolder.GameColor Color => color;
    public int BulletCount => bulletCount;
    public int LinkGroupId => linkGroupId;
    public int LaneIndex => laneIndex;
    public int OrderIndex => orderIndex;

    public ShooterState State => state;

    public BottomSlotNode CurrentBottomNode => currentBottomNode;
    public MiddleSlotNode CurrentMiddleNode => currentMiddleNode;
    public ShooterLinkGroup LinkGroup => linkGroup;

    public bool IsDestroyed => state == ShooterState.Destroyed;
    public bool HasBullet => bulletCount > 0;
    public bool IsTransferLocked => isTransferLocked;

    public bool IsSelectable =>
        !isTransferLocked &&
        (state == ShooterState.IdleInBottom ||
         state == ShooterState.IdleInMiddle);

    private void Awake()
    {
        CacheAnimationDefaults();

        if (firePoint == null)
        {
            firePoint = transform;
        }

        if (bulletCountText == null)
        {
            bulletCountText = GetComponentInChildren<TMP_Text>(true);
        }

        if (linkLineRenderer == null && linkLineObject != null)
        {
            linkLineRenderer = linkLineObject.GetComponent<LineRenderer>();
        }

        if (outline == null)
        {
            outline = GetComponent<Outlinable>();
        }

        UpdateOutlineVisual();

        ClearLinkVisual();
        UpdateHatVisual();
        UpdateBulletCountText();
        UpdateBulletCountTextAlpha();
    }
    private void CacheAnimationDefaults()
    {
        if (animationDefaultsCached)
            return;

        if (firePoint == null)
        {
            firePoint = transform;
        }

        if (mouthTransform != null)
        {
            mouthDefaultLocalPosition = mouthTransform.localPosition;
        }

        if (leftArmTransform != null)
        {
            leftArmDefaultLocalRotation = leftArmTransform.localRotation;
        }

        if (rightArmTransform != null)
        {
            rightArmDefaultLocalRotation = rightArmTransform.localRotation;
        }

        if (breathingRoot == null)
        {
            breathingRoot = transform;
        }

        if (shootScaleRoot == null)
        {
            shootScaleRoot = breathingRoot != null ? breathingRoot : transform;
        }

        breathingDefaultLocalScale = GetSafeDefaultScaleForRoot(breathingRoot);
        shootScaleDefaultLocalScale = GetSafeDefaultScaleForRoot(shootScaleRoot);

        ApplyDefaultScaleToScaleRoots();

        animationDefaultsCached = true;
    }

    private Vector3 GetSafeDefaultScaleForRoot(Transform targetRoot)
    {
        if (useFixedDefaultScale)
        {
            return fixedDefaultScale == Vector3.zero ? Vector3.one : fixedDefaultScale;
        }

        if (targetRoot == null)
        {
            return Vector3.one;
        }

        return targetRoot.localScale == Vector3.zero ? Vector3.one : targetRoot.localScale;
    }

    private void ApplyDefaultScaleToScaleRoots()
    {
        if (breathingRoot != null)
        {
            breathingRoot.localScale = breathingDefaultLocalScale;
        }

        if (shootScaleRoot != null && shootScaleRoot != breathingRoot)
        {
            shootScaleRoot.localScale = shootScaleDefaultLocalScale;
        }
    }

    public void ForceResetScaleAnimations(bool restartBreathing = true)
    {
        CacheAnimationDefaults();

        if (breathingTween != null)
        {
            breathingTween.Kill(false);
            breathingTween = null;
        }

        if (shootReactionSequence != null)
        {
            shootReactionSequence.Kill(false);
            shootReactionSequence = null;
        }

        if (lockedClickSequence != null)
        {
            lockedClickSequence.Kill(false);
            lockedClickSequence = null;
        }

        if (breathingRoot != null)
        {
            breathingRoot.DOKill(false);
            breathingRoot.localScale = breathingDefaultLocalScale;
        }

        if (shootScaleRoot != null)
        {
            shootScaleRoot.DOKill(false);
            shootScaleRoot.localScale = shootScaleDefaultLocalScale;
        }

        if (restartBreathing && Application.isPlaying)
        {
            UpdateBreathingAnimation();
        }
    }

    private void Update()
    {
        if (state == ShooterState.OnFlower)
        {
            TryShootByRaycast();
        }

        UpdateLinkVisualLine();
    }

    public void Setup(ShooterSpawnData data, GameColors gameColors = null)
    {
        CacheAnimationDefaults();

        color = data.color;
        bulletCount = data.bulletCount;
        linkGroupId = data.linkGroupId;
        laneIndex = data.laneIndex;
        orderIndex = data.orderIndex;

        isHidden = data.isHidden;
        isRevealed = !isHidden;
        cachedGameColors = gameColors;

        state = ShooterState.None;

        ClearRoundHitColumns();

        if (gameColors != null)
        {
            UpdateShooterVisual();
        }

        UpdateBulletCountText();
        UpdateBulletCountTextAlpha();
        UpdateOutlineVisual();


        if (Application.isPlaying)
        {
            UpdateBreathingAnimation();
        }
    }

    public void SetState(ShooterState newState)
    {
        state = newState;

        if (newState == ShooterState.IdleInBottom ||
            newState == ShooterState.IdleInMiddle ||
            newState == ShooterState.Locked)
        {
            UnlockTransfer();
        }
        else
        {
            LockTransfer();
        }

        if (ShouldRevealForState(newState))
        {
            RevealIfHidden();
        }
        else
        {
            UpdateShooterVisual();
        }

        UpdateBulletCountText();
        UpdateBulletCountTextAlpha();
        UpdateOutlineVisual();

        if (Application.isPlaying)
        {
            CacheAnimationDefaults();
            UpdateBreathingAnimation();
            RefreshBottomIdleLookAnimation();
        }
    }

    public void SetBottomNode(BottomSlotNode node)
    {
        currentBottomNode = node;

        if (node != null)
        {
            currentMiddleNode = null;
            //SetState(ShooterState.IdleInBottom);
        }
        RefreshBottomIdleLookAnimation();
    }

    public void SetMiddleNode(MiddleSlotNode node)
    {
        StopBottomIdleLookAnimation(true);

        currentMiddleNode = node;

        if (node != null)
        {
            currentBottomNode = null;
            SetState(ShooterState.IdleInMiddle);
        }
    }

    public void SetLinkGroup(ShooterLinkGroup group)
    {
        linkGroup = group;
        UpdateHatVisual();
    }

    public void ClearPlacement()
    {
        StopBottomIdleLookAnimation(false);

        currentBottomNode = null;
        currentMiddleNode = null;
    }

    public void OnClicked()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGamePlayable)
        {
            return;
        }

        if (isTransferLocked)
        {
            return;
        }

        if (!IsSelectable)
        {
            PlayLockedClickFeedback();
            return;
        }

        if (LevelManager.instance.isTutorialOn)
        {
            TutorialController.instance.HandleInput(StepType.Classic);
        }

        StopBottomIdleLookAnimation(true);

        if (ShooterTransferQueue.Instance == null)
        {
            Debug.LogError("[Shooter] ShooterTransferQueue.Instance bulunamadi.");
            return;
        }

        if (linkGroup != null)
        {
            if (!linkGroup.CanSendGroupToFlower())
            {
                PlayLockedClickFeedback();
                return;
            }

            PlayShooterClickVibration();

            linkGroup.TrySendGroupToFlower(this);
            return;
        }

        PlayShooterClickVibration();

        LockTransfer();

        ShooterTransferRequest request = ShooterTransferRequest.CreateSingle(this);
        ShooterTransferQueue.Instance.Enqueue(request);
    }

    private void PlayLockedClickFeedback()
    {
        PlayLockedClickFeedbackAnimation();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySound(lockedClickSound);
        }

        if (useLockedClickVibration && VibrationManager.instance != null)
        {
            VibrationManager.instance.LockedShooterClick();
        }
    }
    private void PlayShooterClickVibration()
    {
        if (VibrationManager.instance != null)
        {
            VibrationManager.instance.ShooterClick();
        }
    }

    public void StartFlowerRound()
    {
        StopBottomIdleLookAnimation(false);

        ClearRoundHitColumns();
        SetState(ShooterState.OnFlower);
    }

    public void EndFlowerRound()
    {
        ClearRoundHitColumns();
    }

    public bool CanHitColumnThisRound(int columnIndex)
    {
        return !hitColumnsThisRound.Contains(columnIndex);
    }

    public void RegisterColumnHit(int columnIndex)
    {
        hitColumnsThisRound.Add(columnIndex);
    }

    public void ConsumeBullet()
    {
        if (bulletCount <= 0)
            return;

        bulletCount--;

        UpdateBulletCountText();

        if (bulletCount <= 0)
        {
            DestroyShooter();
        }
    }

    private void TryShootByRaycast()
    {
        if (!HasBullet)
            return;

        if (firePoint == null)
            return;

        if (rayTargetPoint == null)
        {
            Debug.LogWarning("[Shooter] RayTargetPoint atanmadý. Ray atýlmadý.");
            return;
        }


        Vector3 rayStart = firePoint.position;
        Vector3 rayDirection = (rayTargetPoint.position - rayStart).normalized;

        Debug.DrawRay(
            rayStart,
            rayDirection * rayDistance,
            UnityEngine.Color.red,
            0f
        );

        RaycastHit[] hits = Physics.RaycastAll(
        rayStart,
        rayDirection,
        rayDistance,
        boxLayerMask
    );
        if (hits == null || hits.Length == 0)
            return;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));



        foreach (RaycastHit hit in hits)
        {
            Box box = hit.collider.GetComponentInParent<Box>();

            if (box == null)
                continue;

            if (BoxGridManager.Instance == null)
                return;

            bool reserved = BoxGridManager.Instance.TryReserveShot(this, box);

            if (!reserved)
                continue;

            FireBulletToBox(box, hit.point);

            Debug.Log($"[Shooter Bullet] Fire Box X:{box.GridX} Y:{box.GridY} Color:{box.Color}");

            return;
        }
    }
    public void DestroyShooter()
    {
        if (state == ShooterState.Destroyed)
            return;

        state = ShooterState.Destroyed;
        LockTransfer();

        OnShooterDestroyed?.Invoke(this);

        if (currentBottomNode != null)
        {
            currentBottomNode.ClearShooter();
        }

        if (currentMiddleNode != null)
        {
            currentMiddleNode.ClearShooter();
        }

        if (linkGroup != null)
        {
            linkGroup.NotifyShooterDestroyed(this);
        }

        ClearLinkVisual();
        StopBottomIdleLookAnimation(false);
        StopBreathingAnimation();

        Destroy(gameObject);
    }
    private void OnDisable()
    {
        StopBottomIdleLookAnimation(false);
        ForceResetScaleAnimations(false);

        if (mouthTransform != null)
        {
            mouthTransform.DOKill(false);
            mouthTransform.localPosition = mouthDefaultLocalPosition;
        }

        ResetArmPose();

        if (linkLineRenderer != null)
        {
            linkLineRenderer.enabled = false;
        }

        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    private void ClearRoundHitColumns()
    {
        hitColumnsThisRound.Clear();
    }

    public void ResetHitColumnsForNewLap()
    {
        ClearRoundHitColumns();
    }

    private void OnMouseDown()
    {
        Debug.Log($"[MeMe Shooter Click] {name} State:{state} IsSelectable:{IsSelectable} Collider click geldi.");

        OnClicked();
    }

    public void ApplyMaterials(GameColors gameColors)
    {
        if (gameColors == null)
        {
            Debug.LogWarning($"[Shooter] GameColors null. Material atanmadý. Shooter: {name}");
            return;
        }

        int colorIndex = (int)color;

        ApplyMaterialArrayToRenderers(
            gameColors.shooterMaterials,
            colorIndex,
            mainColorRenderers,
            "Shooter Main"
        );

        ApplyMaterialArrayToRenderers(
            gameColors.shooterDarkMaterials,
            colorIndex,
            darkColorRenderers,
            "Shooter Dark"
        );
    }

    private void ApplyMaterialArrayToRenderers(
        Material[] materials,
        int colorIndex,
        Renderer[] renderers,
        string materialGroupName)
    {
        if (materials == null)
        {
            Debug.LogWarning($"[Shooter] {materialGroupName} materials null.");
            return;
        }

        if (colorIndex < 0 || colorIndex >= materials.Length)
        {
            Debug.LogWarning($"[Shooter] {materialGroupName} material index out of range: {colorIndex}. Color: {color}");
            return;
        }

        Material targetMaterial = materials[colorIndex];

        if (targetMaterial == null)
        {
            Debug.LogWarning($"[Shooter] {materialGroupName} material null. Color: {color}, Index: {colorIndex}");
            return;
        }

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"[Shooter] {materialGroupName} renderers boþ. Shooter prefabýnda rendererlarý atamalýsýn.");
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.sharedMaterial = targetMaterial;
        }
    }

    private void FireBulletToBox(Box box, Vector3 rayHitPoint)
    {
        if (box == null)
            return;

        PlayMouthPullAnimation();

        Vector3 startPosition = bulletSpawnPoint != null
             ? bulletSpawnPoint.position
             : firePoint != null
                 ? firePoint.position
                 : transform.position;

        //Vector3 targetPosition = box.GetBulletTargetPosition();
        Vector3 targetPosition = rayHitPoint != Vector3.zero
            ? rayHitPoint
            : box.GetBulletTargetPosition();


        if (BulletPool.Instance == null)
        {
            Debug.LogWarning("[Shooter] BulletPool yok. Box direkt kýrýlacak.");
            BoxGridManager.Instance.ApplyReservedHit(box, targetPosition);
            return;
        }
        BulletProjectile bullet = BulletPool.Instance.GetBullet();

        bullet.Fire(
          startPosition,
          targetPosition,
          () =>
          {
              if (BoxGridManager.Instance != null)
              {
                  BoxGridManager.Instance.ApplyReservedHit(box, targetPosition);
              }
          }
      );
    }

    private void PlayMouthPullAnimation()
    {
        ForceResetScaleAnimations(false);

        if (mouthTransform != null)
        {
            mouthTransform.DOKill(false);
            mouthTransform.localPosition = mouthDefaultLocalPosition;
        }

        ResetArmPose();

        Vector3 targetScale = new Vector3(
            shootScaleDefaultLocalScale.x * shootScaleMultiplier.x,
            shootScaleDefaultLocalScale.y * shootScaleMultiplier.y,
            shootScaleDefaultLocalScale.z * shootScaleMultiplier.z
        );

        Quaternion leftArmOpenRotation =
            leftArmDefaultLocalRotation * Quaternion.Euler(shootArmXAngle, 0f, 0f);

        Quaternion rightArmOpenRotation =
            rightArmDefaultLocalRotation * Quaternion.Euler(-shootArmXAngle, 0f, 0f);

        shootReactionSequence = DOTween.Sequence();

        Tween mouthInTween = null;

        if (mouthTransform != null)
        {
            mouthInTween = mouthTransform
                .DOLocalMove(mouthDefaultLocalPosition + mouthPullLocalOffset, mouthPullDuration)
                .SetEase(mouthPullEase);
        }

        if (mouthInTween != null)
        {
            shootReactionSequence.Append(mouthInTween);
        }
        else
        {
            shootReactionSequence.AppendInterval(mouthPullDuration);
        }

        if (shootScaleRoot != null)
        {
            shootReactionSequence.Join(
                shootScaleRoot
                    .DOScale(targetScale, mouthPullDuration)
                    .SetEase(shootScaleInEase)
            );
        }

        if (leftArmTransform != null)
        {
            shootReactionSequence.Join(
                leftArmTransform
                    .DOLocalRotateQuaternion(leftArmOpenRotation, mouthPullDuration)
                    .SetEase(armOpenEase)
            );
        }

        if (rightArmTransform != null)
        {
            shootReactionSequence.Join(
                rightArmTransform
                    .DOLocalRotateQuaternion(rightArmOpenRotation, mouthPullDuration)
                    .SetEase(armOpenEase)
            );
        }

        Tween mouthOutTween = null;

        if (mouthTransform != null)
        {
            mouthOutTween = mouthTransform
                .DOLocalMove(mouthDefaultLocalPosition, mouthPullDuration)
                .SetEase(mouthPullEase);
        }

        if (mouthOutTween != null)
        {
            shootReactionSequence.Append(mouthOutTween);
        }
        else
        {
            shootReactionSequence.AppendInterval(mouthPullDuration);
        }

        if (shootScaleRoot != null)
        {
            shootReactionSequence.Join(
                shootScaleRoot
                    .DOScale(shootScaleDefaultLocalScale, mouthPullDuration)
                    .SetEase(shootScaleOutEase)
            );
        }

        if (leftArmTransform != null)
        {
            shootReactionSequence.Join(
                leftArmTransform
                    .DOLocalRotateQuaternion(leftArmDefaultLocalRotation, mouthPullDuration)
                    .SetEase(armCloseEase)
            );
        }

        if (rightArmTransform != null)
        {
            shootReactionSequence.Join(
                rightArmTransform
                    .DOLocalRotateQuaternion(rightArmDefaultLocalRotation, mouthPullDuration)
                    .SetEase(armCloseEase)
            );
        }

        shootReactionSequence.OnComplete(() =>
        {
            ResetShootReactionToDefault();
            shootReactionSequence = null;

            // Eðer shooter tekrar selectable hale gelmiþse breathing geri baþlar.
            UpdateBreathingAnimation();
        });

        shootReactionSequence.OnKill(() =>
        {
            ResetShootReactionToDefault();
            shootReactionSequence = null;
        });
    }

    private void ResetShootReactionToDefault()
    {
        if (mouthTransform != null)
        {
            mouthTransform.localPosition = mouthDefaultLocalPosition;
        }

        if (shootScaleRoot != null)
        {
            shootScaleRoot.localScale = shootScaleDefaultLocalScale;
        }

        if (breathingRoot != null)
        {
            breathingRoot.localScale = breathingDefaultLocalScale;
        }

        ResetArmPose();
    }

    private void UpdateBulletCountText()
    {
        if (bulletCountText == null)
            return;

        bulletCountText.text = IsHiddenAndNotRevealed()
            ? "?"
            : bulletCount.ToString();
    }

    private void UpdateBulletCountTextAlpha()
    {
        if (bulletCountText == null)
            return;

        Color color = bulletCountText.color;
        color.a = IsSelectable
            ? selectableTextAlpha / 255f
            : lockedTextAlpha / 255f;

        bulletCountText.color = color;
    }
    private void UpdateOutlineVisual()
    {
        if (outline == null)
            return;

        outline.enabled = IsSelectable;
    }
    public void SetLinkVisualTarget(Shooter target)
    {
        linkVisualTarget = target;

        if (linkLineObject != null)
        {
            linkLineObject.SetActive(target != null);
        }

        if (linkLineRenderer != null)
        {
            linkLineRenderer.enabled = target != null;
            linkLineRenderer.positionCount = 2;
            linkLineRenderer.useWorldSpace = true;
        }

        ApplyLinkLineGradient();
        UpdateLinkVisualLine();
        UpdateHatVisual();
    }

    public void ClearLinkVisual()
    {
        linkVisualTarget = null;

        if (linkLineObject != null)
        {
            linkLineObject.SetActive(false);
        }

        if (linkLineRenderer != null)
        {
            linkLineRenderer.enabled = false;
        }
        UpdateHatVisual();
    }


    private void UpdateLinkVisualLine()
    {
        if (linkVisualTarget == null)
            return;

        if (linkLineRenderer == null)
            return;

        Transform startPoint = linkLinePoint != null
            ? linkLinePoint
            : transform;

        Transform targetPoint = linkVisualTarget.linkLinePoint != null
            ? linkVisualTarget.linkLinePoint
            : linkVisualTarget.transform;

        Vector3 startPosition = startPoint.position;
        Vector3 endPosition = targetPoint.position;

        Vector3 transitionStart = Vector3.Lerp(startPosition, endPosition, 0.49f);
        Vector3 transitionEnd = Vector3.Lerp(startPosition, endPosition, 0.51f);

        linkLineRenderer.positionCount = 4;
        linkLineRenderer.useWorldSpace = true;

        linkLineRenderer.SetPosition(0, startPosition);
        linkLineRenderer.SetPosition(1, transitionStart);
        linkLineRenderer.SetPosition(2, transitionEnd);
        linkLineRenderer.SetPosition(3, endPosition);

        ApplyLinkLineGradient();
    }

    private void UpdateHatVisual()
    {
        if (hatObject == null)
            return;

        bool hasAnyConnection =
            linkGroup != null ||
            linkVisualTarget != null;

        hatObject.SetActive(hasAnyConnection);
    }

    public void PlayJumpArmAnimation(float duration)
    {
        if (duration <= 0f)
            return;

        AnimateArm(
            leftArmTransform,
            leftArmDefaultLocalRotation,
            armOpenXAngle,
            duration
        );

        AnimateArm(
            rightArmTransform,
            rightArmDefaultLocalRotation,
            -armOpenXAngle,
            duration
        );
    }

    public void ResetArmPose()
    {
        if (leftArmTransform != null)
        {
            leftArmTransform.DOKill();
            leftArmTransform.localRotation = leftArmDefaultLocalRotation;
        }

        if (rightArmTransform != null)
        {
            rightArmTransform.DOKill();
            rightArmTransform.localRotation = rightArmDefaultLocalRotation;
        }
    }

    private void AnimateArm(
        Transform armTransform,
        Quaternion defaultLocalRotation,
        float xAngle,
        float totalDuration)
    {
        if (armTransform == null)
            return;

        armTransform.DOKill();

        armTransform.localRotation = defaultLocalRotation;

        float openDuration = totalDuration * armOpenDurationRatio;
        float closeDuration = totalDuration - openDuration;

        Quaternion openedRotation =
            defaultLocalRotation * Quaternion.Euler(xAngle, 0f, 0f);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            armTransform
                .DOLocalRotateQuaternion(openedRotation, openDuration)
                .SetEase(armOpenEase)
        );

        sequence.Append(
            armTransform
                .DOLocalRotateQuaternion(defaultLocalRotation, closeDuration)
                .SetEase(armCloseEase)
        );

        sequence.OnComplete(() =>
        {
            armTransform.localRotation = defaultLocalRotation;
        });
    }

    private void UpdateBreathingAnimation()
    {
        if (IsSelectable)
        {
            StartBreathingAnimation();
        }
        else
        {
            StopBreathingAnimation();
        }
    }

    private void StartBreathingAnimation()
    {
        CacheAnimationDefaults();

        if (breathingRoot == null)
            return;

        if (breathingTween != null && breathingTween.IsActive())
            return;

        Vector3 targetScale = new Vector3(
            breathingDefaultLocalScale.x * breathingScaleMultiplier.x,
            breathingDefaultLocalScale.y * breathingScaleMultiplier.y,
            breathingDefaultLocalScale.z * breathingScaleMultiplier.z
        );

        breathingRoot.localScale = breathingDefaultLocalScale;

        breathingTween = breathingRoot
            .DOScale(targetScale, breathingDuration)
            .SetEase(breathingEase)
            .SetLoops(-1, LoopType.Yoyo);
    }


    private void StopBreathingAnimation()
    {
        if (breathingTween != null)
        {
            breathingTween.Kill(false);
            breathingTween = null;
        }

        if (breathingRoot != null)
        {
            breathingRoot.DOKill(false);
            breathingRoot.localScale = breathingDefaultLocalScale;
        }
    }

    private void RefreshBottomIdleLookAnimation()
    {
        if (ShouldRunBottomIdleLookAnimation())
        {
            StartBottomIdleLookLoop();
        }
        else
        {
            StopBottomIdleLookAnimation(true);
        }
    }

    private bool ShouldRunBottomIdleLookAnimation()
    {
        if (!enableBottomIdleLookAnimation)
            return false;

        if (currentBottomNode == null)
            return false;

        if (IsDestroyed)
            return false;

        if (isBottomIdleLookPlaying)
            return true;

        return state == ShooterState.IdleInBottom ||
               state == ShooterState.Locked;
    }

    private void StartBottomIdleLookLoop()
    {
        if (bottomIdleLookCoroutine != null)
            return;

        bottomIdleLookCoroutine = StartCoroutine(BottomIdleLookRoutine());
    }

    private IEnumerator BottomIdleLookRoutine()
    {
        while (ShouldRunBottomIdleLookAnimation())
        {
            float waitTime = UnityEngine.Random.Range(
                bottomIdleLookDelayRange.x,
                bottomIdleLookDelayRange.y
            );

            float timer = 0f;

            while (timer < waitTime)
            {
                if (!ShouldRunBottomIdleLookAnimation())
                {
                    bottomIdleLookCoroutine = null;
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            yield return PlayBottomIdleLookOnceRoutine();
        }

        bottomIdleLookCoroutine = null;
    }

    private IEnumerator PlayBottomIdleLookOnceRoutine()
    {
        if (currentBottomNode == null)
            yield break;

        isBottomIdleLookPlaying = true;

        Transform shooterTransform = transform;

        shooterTransform.DOKill();

        Vector3 targetPosition = currentBottomNode.transform.position;
        Quaternion frontRotation = currentBottomNode.transform.rotation;
        Quaternion backRotation = frontRotation * Quaternion.Euler(0f, 180f, 0f);

        bool firstJumpCompleted = false;

        PlayJumpArmAnimation(bottomIdleLookTurnDuration);

        bottomIdleLookSequence = DOTween.Sequence();

        bottomIdleLookSequence.Join(
            shooterTransform
                .DOJump(
                    targetPosition,
                    bottomIdleLookJumpPower,
                    bottomIdleLookJumpCount,
                    bottomIdleLookTurnDuration
                )
                .SetEase(bottomIdleLookJumpEase)
        );

        bottomIdleLookSequence.Join(
            shooterTransform
                .DORotateQuaternion(backRotation, bottomIdleLookTurnDuration)
                .SetEase(bottomIdleLookRotateEase)
        );

        bottomIdleLookSequence.OnComplete(() =>
        {
            shooterTransform.position = targetPosition;
            shooterTransform.rotation = backRotation;
            ResetArmPose();
            firstJumpCompleted = true;
        });

        yield return new WaitUntil(() => firstJumpCompleted);

        float holdTimer = 0f;

        while (holdTimer < bottomIdleLookBackHoldDuration)
        {
            if (!ShouldRunBottomIdleLookAnimation())
            {
                isBottomIdleLookPlaying = false;
                yield break;
            }

            holdTimer += Time.deltaTime;
            yield return null;
        }

        bool secondJumpCompleted = false;

        PlayJumpArmAnimation(bottomIdleLookTurnDuration);

        bottomIdleLookSequence = DOTween.Sequence();

        bottomIdleLookSequence.Join(
            shooterTransform
                .DOJump(
                    targetPosition,
                    bottomIdleLookJumpPower,
                    bottomIdleLookJumpCount,
                    bottomIdleLookTurnDuration
                )
                .SetEase(bottomIdleLookJumpEase)
        );

        bottomIdleLookSequence.Join(
            shooterTransform
                .DORotateQuaternion(frontRotation, bottomIdleLookTurnDuration)
                .SetEase(bottomIdleLookRotateEase)
        );

        bottomIdleLookSequence.OnComplete(() =>
        {
            shooterTransform.position = targetPosition;
            shooterTransform.rotation = frontRotation;
            ResetArmPose();
            secondJumpCompleted = true;
        });

        yield return new WaitUntil(() => secondJumpCompleted);

        isBottomIdleLookPlaying = false;
    }

    public void StopBottomIdleLookAnimation(bool resetToBottomNodeRotation)
    {
        if (bottomIdleLookCoroutine != null)
        {
            StopCoroutine(bottomIdleLookCoroutine);
            bottomIdleLookCoroutine = null;
        }

        if (bottomIdleLookSequence != null)
        {
            bottomIdleLookSequence.Kill();
            bottomIdleLookSequence = null;
        }

        isBottomIdleLookPlaying = false;

        ResetArmPose();

        if (resetToBottomNodeRotation && currentBottomNode != null)
        {
            transform.position = currentBottomNode.transform.position;
            transform.rotation = currentBottomNode.transform.rotation;
        }
    }


    private bool IsHiddenAndNotRevealed()
    {
        return isHidden && !isRevealed;
    }

    private bool ShouldRevealForState(ShooterState newState)
    {
        return newState == ShooterState.IdleInBottom ||
               newState == ShooterState.IdleInMiddle;
    }

    private void RevealIfHidden()
    {
        if (!IsHiddenAndNotRevealed())
            return;

        isRevealed = true;

        UpdateShooterVisual();
        UpdateBulletCountText();
    }

    private void UpdateShooterVisual()
    {
        if (cachedGameColors == null)
            return;

        if (IsHiddenAndNotRevealed())
        {
            ApplyHiddenMaterial(cachedGameColors);
        }
        else
        {
            ApplyMaterials(cachedGameColors);
        }

        ApplyLinkLineGradient();
    }

    private void ApplyHiddenMaterial(GameColors gameColors)
    {
        if (gameColors == null)
            return;

        if (gameColors.shooterMaterials == null ||
            gameColors.shooterMaterials.Length == 0 ||
            gameColors.shooterMaterials[0] == null)
        {
            Debug.LogWarning("[Shooter] Hidden main material için GameColors.shooterMaterials[0] bulunamadý.");
            return;
        }

        Material hiddenMainMaterial = gameColors.shooterMaterials[0];
        Material hiddenDarkMaterial = hiddenMainMaterial;

        if (gameColors.shooterDarkMaterials != null &&
            gameColors.shooterDarkMaterials.Length > 0 &&
            gameColors.shooterDarkMaterials[0] != null)
        {
            hiddenDarkMaterial = gameColors.shooterDarkMaterials[0];
        }

        ApplySingleMaterialToRenderers(hiddenMainMaterial, mainColorRenderers, "Hidden Main");
        ApplySingleMaterialToRenderers(hiddenDarkMaterial, darkColorRenderers, "Hidden Dark");
    }

    private void ApplySingleMaterialToRenderers(Material material, Renderer[] renderers, string groupName)
    {
        if (material == null)
        {
            Debug.LogWarning($"[Shooter] {groupName} material null. Shooter:{name}");
            return;
        }

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"[Shooter] {groupName} renderer listesi boþ. Shooter:{name}");
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.sharedMaterial = material;
        }
    }

    private void PlayLockedClickFeedbackAnimation()
    {
        CacheAnimationDefaults();

        Transform targetRoot = breathingRoot != null
            ? breathingRoot
            : transform;

        if (targetRoot == null)
            return;

        if (lockedClickSequence != null)
        {
            lockedClickSequence.Kill(false);
            lockedClickSequence = null;
        }

        targetRoot.DOKill(false);

        Vector3 defaultScale = targetRoot == shootScaleRoot
            ? shootScaleDefaultLocalScale
            : breathingDefaultLocalScale;

        if (defaultScale == Vector3.zero)
        {
            defaultScale = Vector3.one;
        }

        targetRoot.localScale = defaultScale;

        Vector3 targetScale = new Vector3(
            defaultScale.x * lockedClickScaleMultiplier.x,
            defaultScale.y * lockedClickScaleMultiplier.y,
            defaultScale.z * lockedClickScaleMultiplier.z
        );

        lockedClickSequence = DOTween.Sequence();

        lockedClickSequence.Append(
            targetRoot
                .DOScale(targetScale, lockedClickScaleDuration)
                .SetEase(lockedClickScaleInEase)
        );

        lockedClickSequence.Append(
            targetRoot
                .DOScale(defaultScale, lockedClickScaleDuration)
                .SetEase(lockedClickScaleOutEase)
        );

        lockedClickSequence.OnComplete(() =>
        {
            targetRoot.localScale = defaultScale;
            lockedClickSequence = null;

            UpdateBreathingAnimation();
        });

        lockedClickSequence.OnKill(() =>
        {
            if (targetRoot != null)
            {
                targetRoot.localScale = defaultScale;
            }

            lockedClickSequence = null;
        });
    }
    public void SetTransferLocked(bool value)
    {
        isTransferLocked = value;
        UpdateBulletCountTextAlpha();
        UpdateOutlineVisual();
    }

    public void UnlockTransfer()
    {
        SetTransferLocked(false);
    }

    public void LockTransfer()
    {
        SetTransferLocked(true);
    }

    private UnityEngine.Color GetVisibleLinkColor()
    {
        if (cachedGameColors == null)
            return UnityEngine.Color.white;

        if (cachedGameColors.activeColors == null || cachedGameColors.activeColors.Length == 0)
            return UnityEngine.Color.white;

        int colorIndex = IsHiddenAndNotRevealed()
            ? 0
            : (int)color;

        if (colorIndex < 0 || colorIndex >= cachedGameColors.activeColors.Length)
            return UnityEngine.Color.white;

        UnityEngine.Color result = cachedGameColors.activeColors[colorIndex];
        result.a = 1f;

        return result;
    }

    private void ApplyLinkLineGradient()
    {
        if (linkLineRenderer == null)
            return;

        if (linkVisualTarget == null)
            return;

        UnityEngine.Color startColor = GetVisibleLinkColor();
        UnityEngine.Color endColor = linkVisualTarget.GetVisibleLinkColor();

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
            new GradientColorKey(startColor, 0f),
            new GradientColorKey(startColor, 0.40f),
            new GradientColorKey(endColor, 0.60f),
            new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[]
            {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 0.40f),
            new GradientAlphaKey(1f, 0.60f),
            new GradientAlphaKey(1f, 1f)
            }
        );

        linkLineRenderer.colorGradient = gradient;
    }
}