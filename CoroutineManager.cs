using System;
using System.Collections;
using System.Windows.Media;

namespace UnityCoroutines
{
    public class CoroutineManager
    {
        private Coroutine first;
        private int currentFrame;
        private float currentTime;

        public delegate void Update();
        public Update OnUpdate;


        public static CoroutineManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new CoroutineManager();
                }
                return m_Instance;
            }
        }

        private static CoroutineManager m_Instance;

        private CoroutineManager() { }

        private bool isRunning = false;

        public void Run()
        {
            if (isRunning)
                return;
            isRunning = true;
            Time.Run();
            CompositionTarget.Rendering += ExecuteUpdates; 
            OnUpdate += UpdateCoroutines;
        }

        private void ExecuteUpdates(object sender, EventArgs e)
        {
            OnUpdate();
        }

        void UpdateCoroutines()
        {
            currentFrame++;
            UpdateAllCoroutines(currentFrame, Time.time);
        }

        public Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                return null;
            }

            Coroutine newCoroutine = new Coroutine(coroutine);
            AddCoroutine(newCoroutine);
            return newCoroutine;
        }

        private void AddCoroutine(Coroutine coroutine)
        {

            if (first != null)
            {
                coroutine.listNext = first;
                first.listPrevious = coroutine;
            }
            first = coroutine;
        }

        private void RemoveCoroutine(Coroutine coroutine)
        {
            if (first == coroutine)
            {
                first = coroutine.listNext;
            }
            else
            {
                if (coroutine.listNext != null)
                {
                    coroutine.listPrevious.listNext = coroutine.listNext;
                    coroutine.listNext.listPrevious = coroutine.listPrevious;
                }
                else if (coroutine.listPrevious != null)
                {
                    coroutine.listPrevious.listNext = null;
                }
            }
            coroutine.listPrevious = null;
            coroutine.listNext = null;
        }

        public void UpdateAllCoroutines(int frame, float time)
        {
            Coroutine coroutine = first;
            currentTime = time;
            while (coroutine != null)
            {
                Coroutine listNext = coroutine.listNext;

                if (coroutine.waitForFrame > 0 && frame >= coroutine.waitForFrame)
                {
                    coroutine.waitForFrame = -1;
                    UpdateCoroutine(coroutine);
                }
                else if (coroutine.waitForTime > 0.0f && time >= coroutine.waitForTime)
                {
                    coroutine.waitForTime = -1.0f;
                    UpdateCoroutine(coroutine);
                }
                else if (coroutine.waitForCoroutine != null && coroutine.waitForCoroutine.finished)
                {
                    coroutine.waitForCoroutine = null;
                    UpdateCoroutine(coroutine);
                }
                else if (coroutine.waitForObject != null && coroutine.waitForObject.finished)
                {
                    coroutine.waitForObject = null;
                    UpdateCoroutine(coroutine);
                }
                else if (coroutine.waitForFrame == -1 && coroutine.waitForTime == -1.0f
                         && coroutine.waitForCoroutine == null && coroutine.waitForObject == null)
                {
                    UpdateCoroutine(coroutine);
                }
                coroutine = listNext;
            }
        }

        private void UpdateCoroutine(Coroutine coroutine)
        {
            IEnumerator fiber = coroutine.fiber;
            if (coroutine.fiber.MoveNext())
            {
                object yieldCommand = fiber.Current == null ? 1 : fiber.Current;

                if (yieldCommand.GetType() == typeof(int))
                {
                    coroutine.waitForFrame = (int)yieldCommand;
                    coroutine.waitForFrame += currentFrame;
                }
                else if (yieldCommand.GetType() == typeof(float))
                {
                    coroutine.waitForTime = (float)yieldCommand;
                    coroutine.waitForTime += currentTime;
                }
                else if (yieldCommand.GetType() == typeof(Coroutine))
                {
                    coroutine.waitForCoroutine = (Coroutine)yieldCommand;
                }
                else if (yieldCommand is YieldInstruction)
                {
                    coroutine.waitForObject = yieldCommand as YieldInstruction;
                }
                else
                {
                    throw new ArgumentException("Coroutine Manager: Unexpected coroutine yield type: " + yieldCommand.GetType());
                }
            }
            else
            {
                coroutine.finished = true;
                RemoveCoroutine(coroutine);
            }
        }
    }
}
