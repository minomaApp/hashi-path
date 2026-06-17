using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Runtime.LevelCreation;
using UnityEngine;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Data;
using BoxPuller.Scripts.Runtime.Managers;

public class BoxGridManager : MonoBehaviour
{
    public static BoxGridManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, Box> boxesByPosition = new Dictionary<Vector2Int, Box>();
    private readonly Dictionary<Vector2Int, Vector3> cellWorldPositions = new Dictionary<Vector2Int, Vector3>();
    private readonly Dictionary<int, List<Box>> moldGroups = new Dictionary<int, List<Box>>();

    [Header("Visual")]
    [SerializeField] private GameColors gameColors;

    [Header("Collapse Animation")]
    [SerializeField] private float collapseMoveDuration = 0.25f;
    [SerializeField] private Ease collapseEase = Ease.OutBack;

    [Header("Game Flow")]
    [SerializeField] private GameManager gameManager;

    [Header("Win Condition")]
    [SerializeField] private int totalBreakableBoxCount;
    [SerializeField] private int brokenBoxCount;
    [SerializeField] private bool winTriggered;

    [Header("Hit Particle")]
    [SerializeField] private ParticleSystem boxHitParticlePrefab;
    [SerializeField] private Transform particleParent;
    [SerializeField] private float particleDestroyDelay = 2f;

    [Header("Neighbor Impact Shake")]
    [SerializeField] private bool shakeNeighborsOnNormalBoxBreak = true;
    [SerializeField] private bool includeDiagonalImpactNeighbors = false;
    [SerializeField] private bool shakeMoldNeighbors = false;
    [SerializeField] private float neighborShakeDistance = 0.12f;
    [SerializeField] private float neighborShakeDuration = 0.18f;
    [SerializeField] private Ease neighborShakeEase = Ease.OutQuad;

    [Header("Audio")]
    [AudioClipName] public string boxHitSound;

    private void Awake()
    {
        Instance = this;
    }

    public void ConfigureRuntimeReferences(GameManager runtimeGameManager)
    {
        gameManager = runtimeGameManager;
    }

    public void Setup(LevelContainer levelContainer)
    {
        boxesByPosition.Clear();
        cellWorldPositions.Clear();
        moldGroups.Clear();

        totalBreakableBoxCount = 0;
        brokenBoxCount = 0;
        winTriggered = false;

        if (levelContainer == null)
        {
            Debug.LogError("[BoxGridManager] LevelContainer null.");
            return;
        }

        foreach (GameObject boxObject in levelContainer.GeneratedBoxes)
        {
            if (boxObject == null)
                continue;

            Box box = boxObject.GetComponent<Box>();
            GeneratedLevelItem generatedItem = boxObject.GetComponent<GeneratedLevelItem>();

            if (box == null || generatedItem == null)
                continue;

            box.SetupRuntime(
                generatedItem.color,
                generatedItem.x,
                generatedItem.y,
                generatedItem.moldGroupId,
                this,
                gameColors
            );

            totalBreakableBoxCount++;

            Vector2Int gridPos = new Vector2Int(box.GridX, box.GridY);

            boxesByPosition[gridPos] = box;
            cellWorldPositions[gridPos] = box.transform.position;
        }

        BuildMoldGroupsAndVisuals();

        Debug.Log($"[BoxGridManager] Total breakable boxes: {totalBreakableBoxCount}");
    }

    public bool TryHitBox(Shooter shooter, Box box)
    {
        if (!TryReserveShot(shooter, box))
            return false;

        ApplyReservedHit(box);
        return true;
    }

    public bool TryReserveShot(Shooter shooter, Box box)
    {
        if (shooter == null || box == null)
            return false;

        if (box.IsHit || box.IsReserved)
            return false;

        if (box.Color != shooter.Color)
            return false;

        // Ayný shooter ayný flower turu içinde ayný sütundan sadece 1 blok kýrabilir.
        // Böylece alttaki blok kýrýlýnca üstten düþen ayný renk blok ayný turda tekrar kýrýlmaz.
        if (!shooter.CanHitColumnThisRound(box.GridX))
            return false;

        if (!box.ReserveHit())
            return false;

        shooter.RegisterColumnHit(box.GridX);
        shooter.ConsumeBullet();

        return true;
    }
    public void ApplyReservedHit(Box box)
    {
        ApplyReservedHit(box, Vector3.zero);
    }

    public void ApplyReservedHit(Box box, Vector3 hitPosition)
    {
        if (box == null)
            return;

        if (box.IsHit)
            return;

        PlayBoxHitParticle(box, hitPosition);

        PlayBoxHitSound();

        PlayBoxBreakVibration();

        if (box.HasMold)
        {
            HitMoldBox(box);
        }
        else
        {
            RemoveBoxAndCollapse(box);
        }
    }
    private void HitMoldBox(Box box)
    {
        RegisterBoxBroken(box);

        box.HideCubeButKeepMold();

        if (!moldGroups.TryGetValue(box.MoldGroupId, out List<Box> group))
            return;

        if (!IsCompletedMoldGroup(group))
            return;

        HashSet<int> affectedColumns = new HashSet<int>();

        foreach (Box moldBox in group)
        {
            if (moldBox == null)
                continue;

            affectedColumns.Add(moldBox.GridX);

            Vector2Int pos = new Vector2Int(moldBox.GridX, moldBox.GridY);
            boxesByPosition.Remove(pos);

            moldBox.RemoveCompletely();
        }

        moldGroups.Remove(box.MoldGroupId);

        foreach (int column in affectedColumns)
        {
            CollapseColumn(column, true);
        }
    }

    private bool IsCompletedMoldGroup(List<Box> group)
    {
        if (group == null || group.Count == 0)
            return false;

        foreach (Box box in group)
        {
            if (box == null || !box.IsHit)
                return false;
        }

        return true;
    }

    private void RemoveBoxAndCollapse(Box box)
    {
        int column = box.GridX;

        ShakeNeighborBoxes(box);

        Vector2Int pos = new Vector2Int(box.GridX, box.GridY);
        boxesByPosition.Remove(pos);

        RegisterBoxBroken(box);

        box.RemoveCompletely();

        CollapseColumn(column, true);
    }

    private void CollapseColumn(int column, bool processMoldGroupDrops)
    {
        int maxY = GetMaxYForColumn(column);

        if (maxY < 0)
            return;

        int nextFreeY = 0;

        for (int y = 0; y <= maxY; y++)
        {
            Vector2Int currentPos = new Vector2Int(column, y);

            if (!boxesByPosition.TryGetValue(currentPos, out Box box) || box == null)
                continue;

            // Mold box tek baþýna column collapse ile hareket etmez.
            // Kalýp grubu bütün olarak aþaðý inmeli.
            if (box.HasMold)
            {
                nextFreeY = y + 1;
                continue;
            }

            if (box.IsHit)
                continue;

            if (y == nextFreeY)
            {
                nextFreeY++;
                continue;
            }

            Vector2Int oldPos = new Vector2Int(box.GridX, box.GridY);
            boxesByPosition.Remove(oldPos);

            box.SetGridPosition(column, nextFreeY);

            Vector2Int newPos = new Vector2Int(column, nextFreeY);
            boxesByPosition[newPos] = box;

            if (cellWorldPositions.TryGetValue(newPos, out Vector3 targetPosition))
            {
                box.transform.DOKill();

                box.transform
                    .DOMove(targetPosition, collapseMoveDuration)
                    .SetEase(collapseEase);
            }

            nextFreeY++;
        }

        if (processMoldGroupDrops)
        {
            ProcessMoldGroupDrops();
        }
    }

    private int GetMaxYForColumn(int column)
    {
        int maxY = -1;

        foreach (Vector2Int pos in cellWorldPositions.Keys)
        {
            if (pos.x != column)
                continue;

            if (pos.y > maxY)
            {
                maxY = pos.y;
            }
        }

        return maxY;
    }

    private void ProcessMoldGroupDrops()
    {
        bool movedAnyGroup;

        do
        {
            movedAnyGroup = false;

            List<List<Box>> groups = moldGroups.Values
                .Where(group => group != null && group.Count > 0)
                .ToList();

            foreach (List<Box> group in groups)
            {
                int dropDistance = GetMoldGroupDropDistance(group);

                if (dropDistance <= 0)
                    continue;

                HashSet<int> affectedColumns = MoveMoldGroupDown(group, dropDistance);

                foreach (int column in affectedColumns)
                {
                    CollapseColumn(column, false);
                }

                movedAnyGroup = true;
            }
        }
        while (movedAnyGroup);
    }

    private int GetMoldGroupDropDistance(List<Box> group)
    {
        if (group == null || group.Count == 0)
            return 0;

        int dropDistance = 0;

        while (true)
        {
            bool canMove = true;

            foreach (Box box in group)
            {
                if (box == null)
                {
                    canMove = false;
                    break;
                }

                int targetX = box.GridX;
                int targetY = box.GridY - (dropDistance + 1);

                if (targetY < 0)
                {
                    canMove = false;
                    break;
                }

                Vector2Int targetPos = new Vector2Int(targetX, targetY);

                if (!boxesByPosition.TryGetValue(targetPos, out Box otherBox))
                    continue;

                // Kendi mold grubundaki kutular engel sayýlmaz.
                if (group.Contains(otherBox))
                    continue;

                canMove = false;
                break;
            }

            if (!canMove)
                break;

            dropDistance++;
        }

        return dropDistance;
    }

    private HashSet<int> MoveMoldGroupDown(List<Box> group, int dropDistance)
    {
        HashSet<int> affectedColumns = new HashSet<int>();

        if (group == null || group.Count == 0 || dropDistance <= 0)
            return affectedColumns;

        foreach (Box box in group)
        {
            if (box == null)
                continue;

            affectedColumns.Add(box.GridX);

            Vector2Int oldPos = new Vector2Int(box.GridX, box.GridY);
            boxesByPosition.Remove(oldPos);
        }

        foreach (Box box in group)
        {
            if (box == null)
                continue;

            int newX = box.GridX;
            int newY = box.GridY - dropDistance;

            box.SetGridPosition(newX, newY);

            Vector2Int newPos = new Vector2Int(newX, newY);
            boxesByPosition[newPos] = box;

            if (cellWorldPositions.TryGetValue(newPos, out Vector3 targetWorldPos))
            {
                box.transform.DOKill();

                box.transform
                    .DOMove(targetWorldPos, collapseMoveDuration)
                    .SetEase(collapseEase);
            }
        }

        return affectedColumns;
    }

    private void BuildMoldGroupsAndVisuals()
    {
        moldGroups.Clear();

        Dictionary<int, List<Box>> candidateGroups = new Dictionary<int, List<Box>>();

        foreach (Box box in boxesByPosition.Values)
        {
            if (box == null)
                continue;

            if (box.MoldGroupId < 0)
            {
                box.SetValidMoldLinked(false);
                box.SetMoldVisual(false, false);
                continue;
            }

            if (!candidateGroups.ContainsKey(box.MoldGroupId))
            {
                candidateGroups.Add(box.MoldGroupId, new List<Box>());
            }

            candidateGroups[box.MoldGroupId].Add(box);
        }

        foreach (KeyValuePair<int, List<Box>> pair in candidateGroups)
        {
            int moldId = pair.Key;
            List<Box> group = pair.Value;

            if (!IsValidHorizontalMoldGroup(group))
            {
                foreach (Box box in group)
                {
                    if (box == null)
                        continue;

                    box.SetValidMoldLinked(false);
                    box.SetMoldVisual(false, false);
                }

                Debug.LogWarning($"[BoxGridManager] Mold group geçersiz. Id:{moldId}. Kutular tam yan yana deðil.");
                continue;
            }

            List<Box> sortedGroup = group
                .OrderBy(box => box.GridX)
                .ToList();

            moldGroups[moldId] = sortedGroup;

            for (int i = 0; i < sortedGroup.Count; i++)
            {
                Box box = sortedGroup[i];

                bool hasRightConnectedBox = i < sortedGroup.Count - 1;

                box.SetValidMoldLinked(true);

                box.SetMoldVisual(
                    showMold: true,
                    showRightConnector: hasRightConnectedBox
                );
            }
        }
    }

    private bool IsValidHorizontalMoldGroup(List<Box> group)
    {
        if (group == null)
            return false;

        if (group.Count < 2)
            return false;

        int rowY = group[0].GridY;

        foreach (Box box in group)
        {
            if (box == null)
                return false;

            if (box.GridY != rowY)
                return false;
        }

        List<Box> sortedGroup = group
            .OrderBy(box => box.GridX)
            .ToList();

        for (int i = 1; i < sortedGroup.Count; i++)
        {
            int previousX = sortedGroup[i - 1].GridX;
            int currentX = sortedGroup[i].GridX;

            if (currentX != previousX + 1)
                return false;
        }

        return true;
    }

    private void RegisterBoxBroken(Box box)
    {
        if (box == null)
            return;

        brokenBoxCount++;

        Debug.Log($"[BoxGridManager] Box broken: {brokenBoxCount}/{totalBreakableBoxCount}");

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (winTriggered)
            return;

        if (totalBreakableBoxCount <= 0)
            return;

        if (brokenBoxCount < totalBreakableBoxCount)
            return;

        winTriggered = true;

        Debug.Log("[BoxGridManager] All boxes broken. WIN!");

        DOVirtual.DelayedCall(0.5f, () =>
        {
            if (gameManager != null)
            {
                gameManager.GameWin();
                return;
            }

            GameManager foundGameManager = FindFirstObjectByType<GameManager>();

            if (foundGameManager != null)
            {
                foundGameManager.GameWin();
            }
        });
    }

    private void PlayBoxHitParticle(Box box, Vector3 hitPosition)
    {
        if (boxHitParticlePrefab == null)
            return;

        if (box == null)
            return;

        Vector3 spawnPosition = box.GetHitParticlePosition(hitPosition);

        ParticleSystem particle = Instantiate(
            boxHitParticlePrefab,
            spawnPosition,
            Quaternion.identity,
            particleParent
        );

        particle.Play();

        Destroy(particle.gameObject, particleDestroyDelay);
    }
    private void PlayBoxHitSound()
    {
        if (AudioManager.instance == null)
            return;

        AudioManager.instance.PlaySound(boxHitSound);
    }
    private void ShakeNeighborBoxes(Box explodedBox)
    {
        if (!shakeNeighborsOnNormalBoxBreak)
            return;

        if (explodedBox == null)
            return;

        Vector2Int origin = new Vector2Int(explodedBox.GridX, explodedBox.GridY);

        List<Vector2Int> directions = GetImpactNeighborDirections();

        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborPos = origin + direction;

            if (!boxesByPosition.TryGetValue(neighborPos, out Box neighborBox))
                continue;

            if (neighborBox == null)
                continue;

            if (neighborBox == explodedBox)
                continue;

            if (neighborBox.IsHit)
                continue;

            if (neighborBox.HasMold && !shakeMoldNeighbors)
                continue;

            neighborBox.PlayNeighborImpactShake(
                explodedBox.transform.position,
                neighborShakeDistance,
                neighborShakeDuration,
                neighborShakeEase
            );
        }
    }

    private List<Vector2Int> GetImpactNeighborDirections()
    {
        List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

        if (includeDiagonalImpactNeighbors)
        {
            directions.Add(new Vector2Int(1, 1));
            directions.Add(new Vector2Int(1, -1));
            directions.Add(new Vector2Int(-1, 1));
            directions.Add(new Vector2Int(-1, -1));
        }

        return directions;
    }
    private void PlayBoxBreakVibration()
    {
        if (VibrationManager.instance != null)
        {
            VibrationManager.instance.BoxBreak();
        }
    }
}