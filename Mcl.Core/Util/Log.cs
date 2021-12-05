using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Util
{
   public class Log
    {

        protected string _prefix { get; set; }
        protected string _filepath { get; set; }

        public void Write(string str) {
            string time = "[" + DateTime.Now.ToString("T") + "]";
            if (this._prefix == null)
            {
                

                File.AppendAllText(this._filepath, time + str + "\r\n");
            }
            else
            {
                File.AppendAllText(this._filepath, time + "[" + this._prefix + "] " + str + "\r\n");
            }
            return;
        }



        private void Init() {

      

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prefix">日志前缀</param>
        /// <param name="filePath">日志路径</param>
        public Log(string prefix,string filePath) {
            
            this._prefix = prefix;
            this._filepath = filePath;
        }
        public Log(string prefix)
        {
            this._prefix = prefix;
            this._filepath = @".\hook.log"; ;
        }
        public Log()
        {
            this._prefix = "";
            this._filepath = @".\hook.log";
        }

        public static void log(string title,string prefix = "unisdk") {

            Log log = new Log(prefix);
            log.Write(title);
        
        }


    }
}
