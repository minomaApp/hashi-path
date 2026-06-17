using System.Collections;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    public class ProfilerDataCollector : MonoBehaviour
    {
        ProfilerRecorder drawCalls;
        ProfilerRecorder batches;
        ProfilerRecorder gcAlloc;
        ProfilerRecorder physicsTime;
        ProfilerRecorder mainThreadTime;

        void OnEnable()
        {
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            physicsTime = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "FixedUpdate.PhysicsFixedUpdate");
            mainThreadTime = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");

            StartCoroutine(CollectAndSaveData());
        }

        IEnumerator CollectAndSaveData()
        {
            yield return new WaitForSeconds(3f); // Sahne otursun diye 3 saniye bekliyoruz

            ProfilerMetrics metrics = new ProfilerMetrics
            {
                drawCalls = drawCalls.LastValue,
                batches = batches.LastValue,
                gcAllocBytes = gcAlloc.LastValue,
                physicsTimeNs = physicsTime.LastValue,
                mainThreadTimeNs = mainThreadTime.LastValue
            };

            string json = JsonUtility.ToJson(metrics, true);

            string path = Path.Combine(Application.persistentDataPath, "profiler_output.json");
            File.WriteAllText(path, json);

            Debug.Log($"Profiler verisi dışa aktarıldı: {path}");
        }

        void OnDisable()
        {
            drawCalls.Dispose();
            batches.Dispose();
            gcAlloc.Dispose();
            physicsTime.Dispose();
            mainThreadTime.Dispose();
        }

        [System.Serializable]
        public class ProfilerMetrics
        {
            public long drawCalls;
            public long batches;
            public long gcAllocBytes;
            public long physicsTimeNs;
            public long mainThreadTimeNs;
        }
    }
}
