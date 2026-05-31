using MonsterLove.StateMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopTenReasons {
    internal static class MonsterStatesHelper {

        public static void SubscibeOnStateUpdateOnce(StateMapping<MonsterBase.States> stateMapping, Action action) {
            stateMapping.Update += OnStateUpdate;
            stateMapping.ExitCall += OnStateExit;

            void OnStateExit() {
                stateMapping.Update -= OnStateUpdate;
                stateMapping.ExitCall -= OnStateExit;
            }

            void OnStateUpdate() {
                action?.Invoke();
            }
        }
    }
}
