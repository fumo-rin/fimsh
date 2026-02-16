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

    public static Coroutine StartStage(List<FishNode.FishRunDataDTO> dto, DialogueStackSO stack = null)
    {
        List<FishNode.FishRunData> compiled = new();
        foreach (var item in dto)
        {
            if (FishNode.FromDTO(item) is FishNode.FishRunData data)
                compiled.Add(data);
        }
        return StartStage(compiled);
    }
    public static Coroutine StartStage(List<FishNode.FishRunData> fishStage, DialogueStackSO stack = null)
    {
        if (instance is not FishTools f)
        {
            Debug.LogError("Missing Fish Tools instance");
            return null;
        }
        Debug.Log("Starting Stage with Nodes count : " + fishStage.Count);
        Debug.Log(fishStage);
        StopStage();
        FishContinue.LastStage = fishStage;
        int totalFish = 0;
        foreach (var item in fishStage)
        {
            totalFish += item.FishValue;
        }
        IEnumerator StartStage()
        {
            if (stack != null)
            {
                yield return 0.15f.WaitForSeconds();
                stack.StartDialogue(out WaitUntil dialogueWait, null);
                yield return dialogueWait;
            }
            FishCounter.StartSession(totalFish, out WaitUntil w);
            foreach (var item in fishStage.OrderByDescending(x => x.order))
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