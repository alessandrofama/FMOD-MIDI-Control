using System.Diagnostics;

namespace UnityCoroutines
{
    public class Time
    {
        public static float deltaTime;
        public static float timeScale = 1f;
        public static float unscaledDeltaTime;
        public static float time;

        private static Stopwatch stopWatch = new Stopwatch();

        public static void Run()
        {
            CoroutineManager.Instance.OnUpdate += UpdateTime;
            stopWatch.Start();
        }

        public static void UpdateTime()
        {
            stopWatch.Stop();
            unscaledDeltaTime = stopWatch.ElapsedMilliseconds / 1000f;
            deltaTime = unscaledDeltaTime * timeScale.Clamp(0f, float.MaxValue);
            time += unscaledDeltaTime;
            stopWatch.Restart();
        }
    }
}
