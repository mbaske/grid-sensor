using System.Collections;
using System;
using UnityEngine;

namespace MBaske.Sensors.Util
{
    public static class CoroutineUtil
    {
        public static void Start(MonoBehaviour context, IEnumerator coroutine)
        {
            Stop(context, coroutine);

            if (context.isActiveAndEnabled)
            {
                context.StartCoroutine(coroutine);
            }
        }

        public static void Stop(MonoBehaviour context, IEnumerator coroutine)
        {
            if (coroutine != null)
            {
                context.StopCoroutine(coroutine);
            }
        }
    }

    /// <summary>
    /// Invokes callback after specified number of frames.
    /// </summary>
    public class InvokeAfterFrames : CustomYieldInstruction
    {
        public IEnumerator Coroutine { get; private set; }

        private readonly int m_TargetFrameCount;

        /// <summary>
        /// Invokes callback after specified number of frames.
        /// </summary>
        /// <param name="context">The MonoBehaviour</param>
        /// <param name="callback">The callback method</param>
        /// <param name="numberOfFrames">Number of frames to wait</param>
        public InvokeAfterFrames(MonoBehaviour context, Action callback, int numberOfFrames = 1)
        {
            m_TargetFrameCount = Time.frameCount + numberOfFrames;
            Coroutine = FrameCoroutine(callback);
            context.StartCoroutine(Coroutine);
        }

        public override bool keepWaiting => Time.frameCount < m_TargetFrameCount;

        private IEnumerator FrameCoroutine(Action callback)
        {
            yield return this;
            callback.Invoke();
        }
    }
}