using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace CDPPingPong.Core
{
    public enum RegisterTaskStatus { Succeeded, Failed, AlreadyRegistered }
    public enum UnregisterTaskStatus { Succeeded, Failed, NoneFound }
    public class BackgroundTaskHelper
    {
        #region Unregister

        public static bool IsTaskRegistered(string taskName)
        {
            bool retVal = false;
            lock (taskName)
            {
                foreach (var cur in BackgroundTaskRegistration.AllTasks)
                {
                    if (cur.Value.Name == taskName)
                    {
                        retVal = true;
                        break;
                    }
                }
            }
            return retVal;
        }

        public static UnregisterTaskStatus UnregisterAllTasks()
        {
            UnregisterTaskStatus status = UnregisterTaskStatus.Failed;

            try
            {
                if (BackgroundTaskRegistration.AllTasks.Count == 0)
                {
                    status = UnregisterTaskStatus.NoneFound;
                }
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        task.Value.Unregister(true);
                    }
                    if (BackgroundTaskRegistration.AllTasks.Count == 0)
                    {
                        status = UnregisterTaskStatus.Succeeded;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return status;
        }

        public static UnregisterTaskStatus UnregisterTaskByName(string taskName)
        {
            UnregisterTaskStatus status = UnregisterTaskStatus.Failed;

            try
            {
                if (BackgroundTaskRegistration.AllTasks.Count == 0)
                {
                    status = UnregisterTaskStatus.NoneFound;
                }
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (String.Compare(task.Value.Name, taskName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            task.Value.Unregister(true);
                            status = UnregisterTaskStatus.Succeeded;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return status;
        }

        #endregion


        #region RegisterSystemTriggerTask

        public static RegisterTaskStatus RegisterSystemTriggerTask(SystemTriggerType type, string taskName)
        {
            RegisterTaskStatus result = RegisterTaskStatus.Failed;

            if (IsTaskRegistered(taskName))
            {
                result = RegisterTaskStatus.AlreadyRegistered;
            }
            else
            {
                try
                {
                    BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                    builder.SetTrigger(new SystemTrigger(type, false));
                    builder.Name = taskName;
                    builder.Register();
                    result = RegisterTaskStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            return result;
        }

        #endregion


        #region RegisterTask

        public static RegisterTaskStatus RegisterTask<T>(string taskName) where T : IBackgroundTrigger, new()
        {
  
            RegisterTaskStatus result = RegisterTaskStatus.Failed;

            if (IsTaskRegistered(taskName))
            {
                result = RegisterTaskStatus.AlreadyRegistered;
            }
            else
            {
                try
                {
                    BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                    builder.SetTrigger(new T());
                    builder.Name = taskName;
                    builder.Register();
                    result = RegisterTaskStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            return result;
        }

        #endregion


        #region RegisterTaskWithTrigger

        public static RegisterTaskStatus RegisterTaskWithTrigger(string taskName, IBackgroundTrigger trigger)
        {
            RegisterTaskStatus result = RegisterTaskStatus.Failed;

            if (IsTaskRegistered(taskName))
            {
                result = RegisterTaskStatus.AlreadyRegistered;
            }
            else
            {
                try
                {
                    BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                    builder.SetTrigger(trigger);
                    builder.Name = taskName;
                    builder.Register();
                    result = RegisterTaskStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            return result;
        }

        #endregion
    }
}
