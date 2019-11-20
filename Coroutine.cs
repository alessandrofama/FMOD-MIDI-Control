using System.Collections;

namespace UnityCoroutines
{
    public class Coroutine
    {
        public Coroutine listPrevious = null;
        public Coroutine listNext = null;
        public IEnumerator fiber;
        public bool finished = false;
        public int waitForFrame = -1;
        public float waitForTime = -1.0f;
        public Coroutine waitForCoroutine;
        public YieldInstruction waitForObject;

        public Coroutine(IEnumerator _fiber)
        {
            fiber = _fiber;
        }
    }
}
