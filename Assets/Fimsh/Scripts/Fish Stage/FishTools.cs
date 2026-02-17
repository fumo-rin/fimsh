using RinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class FishTools : MonoBehaviour
{
    static FishTools instance;
    private void Awake()
    {
        instance = this;
    }
    const string StageSpawn = "STAGE_SPAWN";
    [SerializeField] List<GameObject> prefabs = new();
    [SerializeField] List<MusicWrapper> musicPiece = new();
    HashSet<GameObject> spawnedObjects = new();
    public bool IsGameScene => gameScene.IsLastLoaded;
    public static bool IsEditing => instance == null ? false : !instance.IsGameScene;
    [SerializeField] ScenePairSO gameScene, sceneSelect;
    public static void EndStage()
    {
        StageRoutines.StopAll();
    }
    static Dictionary<string, GameObject> lookup = new() { };
    public static MusicWrapper GetMusic(int index)
    {
        return instance.musicPiece[index % instance.musicPiece.Count];
    }
    public static GameObject GetItem(string id) => lookup.GetValueOrDefault(id);
    private void Start()
    {
        int iteration = 0;
        foreach (var item in prefabs)
        {
            if (item == null)
            {
                continue;
            }
            lookup[iteration.ToString()] = item;
            iteration++;
        }
    }
    public static IEnumerator SpawnFishSequence(GameObject fish, FishItemNode.FishItemRunData data)
    {
        if (!data.runSeperately)
        {
            yield return StageRoutines.StartRoutine(StageSpawn, CO_Run(), false);
        }
        else
        {
            StageRoutines.StartRoutine(StageSpawn, CO_Run(), false);
            yield return data.addedPostDelay.WaitForSeconds();
        }
        IEnumerator CO_Run()
        {
            GameObject SpawnFish(float x)
            {
                FishSpace.Map(0f, x, out Vector3 startX);
                GameObject spawned = Instantiate(fish, startX, Quaternion.identity);
                return spawned;
            }
            for (int i = 0; i < data.repeats; i++)
            {
                bool notLast = i < data.repeats - 1;
                float lerp = (data.repeats == 1) ? 0f : (float)i / (data.repeats - 1);
                float startX = data.startX.LerpUnclamped(data.endX, lerp);
                float targetX = data.startX.LerpUnclamped(data.endX, lerp);
                GameObject spawned = SpawnFish(startX);
                instance.spawnedObjects.Add(spawned);
                FishSpace.Map(1f, targetX, out Vector3 mappedEnd);
                MoveObject(spawned, mappedEnd, data.fishLerpDuration);
                if (notLast)
                    yield return data.delayBetweenSpawns.WaitForSeconds();
            }
            yield return data.addedPostDelay.WaitForSeconds();
        }
    }
    public struct spawnAndMovePacket
    {
        public float x01Start, x01End, fishLifetime;
        public bool runSeperately;
    }
    public static void SpawnAndMoveItem(GameObject fish, spawnAndMovePacket packet)
    {
        StageRoutines.StartRoutine(StageSpawn, CO_Run(), false);
        GameObject SpawnFish(float x)
        {
            FishSpace.Map(0f, x, out Vector3 startX);
            GameObject spawned = Instantiate(fish, startX, Quaternion.identity);
            return spawned;
        }
        IEnumerator CO_Run()
        {
            GameObject spawned = SpawnFish(packet.x01Start);
            instance.spawnedObjects.Add(spawned);
            FishSpace.Map(1f, packet.x01End, out Vector3 mappedEnd);
            yield return MoveObject(spawned, mappedEnd, packet.fishLifetime);
        }
    }
    public static Coroutine MoveObject(GameObject fish, Vector3 end, float duration)
    {
        IEnumerator CO_Run()
        {
            Vector3 start = fish.transform.position;
            fish.transform.position = start;
            float lerpIncrement = 1f / duration;
            float lerp = 0f;
            while (fish != null && fish.activeInHierarchy && lerp < 1f)
            {
                lerp += lerpIncrement * Time.deltaTime;
                Vector3 processed = start.LerpUnclamped(end, lerp);
                fish.transform.position = processed;
                yield return null;
            }
            if (fish != null && fish.activeInHierarchy && fish.GetComponent<IFibsh>() is IFibsh f && f is FishCollectItem itemFish)
            {
                Destroy(fish);
                instance.spawnedObjects.Remove(fish);
                StopStage();
                FishContinue.Hide();
                FishCounter.StopSession(FishCounter.FishSessionEnd.MissCatch);
                yield return 0.45f.WaitForSeconds();
                FishContinue.Show();
            }
            Destroy(fish);
            instance.spawnedObjects.Remove(fish);
        }
        return StageRoutines.StartRoutine("fish walk", CO_Run(), false);
    }
    static Coroutine runningStage = null;

    public struct stageSettings
    {
        public DialogueStackSO dialogueStack;
        public bool forceActivateNodes;
        public bool displayLevelName;
        public string levelName;
        public stageSettings(bool forceActivateNodes)
        {
            dialogueStack = null;
            this.forceActivateNodes = forceActivateNodes;
            this.displayLevelName = false;
            this.levelName = "";
        }
    }
    public static bool IsStageRunning { get; private set; }
    public static Coroutine StartStage(List<FishNode.FishRunDataDTO> dto, stageSettings settings)
    {
        List<FishNode.FishRunData> compiled = new();
        foreach (var item in dto)
        {
            if (FishNode.FromDTO(item) is FishNode.FishRunData data)
                compiled.Add(data);
        }
        return StartStage(compiled, settings);
    }
    public static Coroutine StartStage(List<FishNode.FishRunData> fishStage, stageSettings settings)
    {
        if (instance is not FishTools f)
        {
            Debug.LogError("Missing Fish Tools instance");
            return null;
        }
        Debug.Log("Starting Stage with Nodes count : " + fishStage.Count);
        StopStage();
        FishContinue.LastStage = fishStage;
        IOrderedEnumerable<FishNode.FishRunData> stage;
        if (settings.forceActivateNodes)
        {
            stage = fishStage.OrderByDescending(x => x.order);
            foreach (var item in stage)
            {
                item.IsActive = true;
            }
        }
        else
        {
            stage = fishStage.Where(x => x.IsActive).OrderByDescending(x => x.order); ;
        }
        int totalFish = 0;
        foreach (var item in stage)
        {
            totalFish += item.FishValue;
        }
        IEnumerator StartStage()
        {
            IsStageRunning = true;
            if (settings.dialogueStack != null)
            {
                yield return 0.15f.WaitForSeconds();
                settings.dialogueStack.StartDialogue(out WaitUntil dialogueWait, null);
                yield return dialogueWait;
            }
            FishCounter.StartSession(totalFish, out WaitUntil w);
            if (settings.displayLevelName)
            {
                FishCounter.SetLevelText(settings.levelName);
            }
            foreach (var item in stage)
            {
                yield return item.RunData();
            }
            yield return w;
            FishCounter.StopSession(FishCounter.FishSessionEnd.CatchAll);
            yield return new WaitUntil(() => IFibsh.TotalFishItems <= 0);
            yield return 1.5f.WaitForSeconds();
            if (instance != null && instance.IsGameScene)
            {
                instance.sceneSelect.Load();
            }
            runningStage = null;
            IsStageRunning = false;
        }
        runningStage = f.StartCoroutine(StartStage());
        return runningStage;
    }
    public static void StopStage()
    {
        if (instance is FishTools f && f.gameObject != null)
        {
            if (runningStage != null)
            {
                IsStageRunning = false;
                StageRoutines.StopRoutine(StageSpawn);
                instance.StopCoroutine(runningStage);
                foreach (var item in f.spawnedObjects.ToList())
                {
                    if (item == null || item.gameObject == null)
                        continue;
                    Destroy(item.gameObject);
                    GeneralManager.FunnyExplosion(new(item.transform.position, true) { scale = RNG.FloatRange(0.65f, 0.75f) });
                }
                f.spawnedObjects.Clear();
            }
        }
    }
}