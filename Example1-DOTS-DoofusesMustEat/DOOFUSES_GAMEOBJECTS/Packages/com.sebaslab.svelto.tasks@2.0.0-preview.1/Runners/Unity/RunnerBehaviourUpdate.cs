#if UNITY_5 || UNITY_5_3_OR_NEWER
using System.Collections;
using Svelto.Common;
using Svelto.DataStructures;
using Svelto.Tasks.Internal;
using UnityEngine;

namespace Svelto.Tasks.Unity.Internal
{
    class RunnerBehaviourUpdate : MonoBehaviour
    {
        void Awake()
        {
            StartCoroutine(CoroutineProcess());
        }

        void Update()
        {
            using (var platform = new PlatformProfiler("Early tasks")) 
                ExecuteRoutines(_earlyProcesses, platform);
            using (var platform = new PlatformProfiler("Update tasks")) 
                ExecuteRoutines(_updateProcesses, platform);
        }

        IEnumerator CoroutineProcess()
        {
            while (true)
            {
                using (var platform = new PlatformProfiler("Coroutine tasks")) 
                    ExecuteRoutines(_coroutineProcesses, platform);

                yield return _waitForEndOfFrame;

                using (var platform = new PlatformProfiler("EndOfFrame tasks")) 
                    ExecuteRoutines(_endOfFrameRoutines, platform);

                yield return null;
            }
        }

        public void OnGUI()
        {
            using (var platform = new PlatformProfiler("onGui tasks")) ExecuteRoutines(_onGuiRoutines, platform);
        }

        void LateUpdate()
        {
            using (var platform = new PlatformProfiler("Late tasks")) ExecuteRoutines(_lateRoutines, platform);
        }

        void FixedUpdate()
        {
            using (var platform = new PlatformProfiler("Physic tasks")) ExecuteRoutines(_physicRoutines, platform);
        }

        static void ExecuteRoutines(ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> list, in PlatformProfiler profiler)
        {
            var orderedRoutines = list.ToArrayFast(out var orderedCount);

            for (int ii = 0; ii < orderedCount; ii++)
            {
                if (orderedRoutines[ii] == null)  continue;

                var routines = orderedRoutines[ii];

                for (int i = 0; i < routines.count; i++)
                {
                    var ret = routines[i].MoveNext(profiler);
                    if (ret == false)
                    {
                        routines.UnorderedRemoveAt((uint)i);
                        i--;
                    }
                }
            }
        }

        public void StartSveltoCoroutine(IProcessSveltoTasks process, uint runningOrder)
        {
            if (_coroutineProcesses.count <= runningOrder || _coroutineProcesses[(int)runningOrder] == null)
                _coroutineProcesses.Add(runningOrder, new FasterList<IProcessSveltoTasks>());
            
            _coroutineProcesses[(int)runningOrder].Add(process);
        }

        public void StartUpdateCoroutine(IProcessSveltoTasks enumerator, uint runningOrder)
        {
            if (_updateProcesses.count <= runningOrder || _updateProcesses[(int)runningOrder] == null)
                _updateProcesses.Add(runningOrder, new FasterList<IProcessSveltoTasks>());

            _updateProcesses[(int)runningOrder].Add(enumerator);
        }
        
        public void StartLateCoroutine(IProcessSveltoTasks enumerator, uint runningOrder)
        {
            if (_lateRoutines.count <= runningOrder || _lateRoutines[(int)runningOrder] == null)
                _lateRoutines.Add(runningOrder, new FasterList<IProcessSveltoTasks>());
            
            _lateRoutines[(int)runningOrder].Add(enumerator);
        }

        public void StartEarlyUpdateCoroutine(IProcessSveltoTasks enumerator, uint runningOrder)
        {
            if (_earlyProcesses.count <= runningOrder || _earlyProcesses[(int)runningOrder] == null)
                _earlyProcesses.Add(runningOrder, new FasterList<IProcessSveltoTasks>());
            
            _earlyProcesses[(int)runningOrder].Add(enumerator);
        }

        public void StartEndOfFrameCoroutine(IProcessSveltoTasks enumerator, uint runningOrder)
        {
            if (_endOfFrameRoutines.count <= runningOrder || _endOfFrameRoutines[(int)runningOrder] == null)
                _endOfFrameRoutines.Add(runningOrder, new FasterList<IProcessSveltoTasks>());
            
            _endOfFrameRoutines[(int)runningOrder].Add(enumerator);
        }

        public void StartOnGuiCoroutine(IProcessSveltoTasks enumerator, uint runningOrder)
        {
            if (_onGuiRoutines.count <= runningOrder || _updateProcesses[(int)runningOrder] == null)
                _onGuiRoutines.Add(runningOrder, new FasterList<IProcessSveltoTasks>());
            
            _onGuiRoutines[(int)runningOrder].Add(enumerator);
        }

        public void StartPhysicCoroutine(IProcessSveltoTasks enumerator, uint runningOrder)
        {
            if (_physicRoutines.count <= runningOrder || _updateProcesses[(int)runningOrder] == null)
                _physicRoutines.Add(runningOrder, new FasterList<IProcessSveltoTasks>());
            
            _physicRoutines[(int)runningOrder].Add(enumerator);
        }

        readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        readonly ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> _earlyProcesses     = new ThreadSafeFasterList<FasterList<IProcessSveltoTasks>>();
        readonly ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> _endOfFrameRoutines = new ThreadSafeFasterList<FasterList<IProcessSveltoTasks>>();
        readonly ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> _updateProcesses    = new ThreadSafeFasterList<FasterList<IProcessSveltoTasks>>();
        readonly ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> _lateRoutines       = new ThreadSafeFasterList<FasterList<IProcessSveltoTasks>>();
        readonly ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> _physicRoutines     = new ThreadSafeFasterList<FasterList<IProcessSveltoTasks>>();
        readonly ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> _coroutineProcesses = new ThreadSafeFasterList<FasterList<IProcessSveltoTasks>>();
        readonly ThreadSafeFasterList<FasterList<IProcessSveltoTasks>> _onGuiRoutines      = new ThreadSafeFasterList<FasterList<IProcessSveltoTasks>>();
    }
}
#endif
