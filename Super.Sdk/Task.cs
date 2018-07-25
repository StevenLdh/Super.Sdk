using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Web;
namespace Super.Sdk
{
    public class Task
    {
        private System.Threading.Tasks.Task task;
        public static Task Run(Action act)
        {
            Task task = new Task();

            var reqid = HttpContext.Current.Items["cc_request_id"];

            task.task = System.Threading.Tasks.Task.Run(() =>
            {

                CallContext.SetData("cc_request_id", reqid);

                act();

            });

            return task;
        }

        public static void WaitAll(params Task[] tasks)
        {
            System.Threading.Tasks.Task[] list = new System.Threading.Tasks.Task[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                list[i] = tasks[i].task;
            }
            System.Threading.Tasks.Task.WaitAll(list);
        }
    }
}
