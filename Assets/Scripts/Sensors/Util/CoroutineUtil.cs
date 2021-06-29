using System.Collections;
using System;
using UnityEngine;

namespace MBaske.Sensors.Util
{
    /// <summary>
    /// Invokes callback after specified number of frames.
    /// </summary>
    // TODO Probably not the best idea to put a coroutine INSIDE
    // of a yield instruction, but it allows for a nice one-liner.
    public class InvokeAfterFrames : CustomYieldInstruction
    {
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
            context.StartCoroutine(Coroutine(callback));
        }

        public override bool keepWaiting => Time.frameCount < m_TargetFrameCount;

        private IEnumerator Coroutine(Action callback)
        {
            yield return this;
            callback.Invoke();
        }
    }
}