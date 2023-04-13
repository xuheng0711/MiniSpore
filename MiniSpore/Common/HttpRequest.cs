using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Common
{
    public class HttpRequest
    {        /// <summary>
             /// Get请求
             /// </summary>
             /// <param name="url"></param>
             /// <returns></returns>
        public string Get(string url)
        {
            string responseText = "";
            try
            {
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create(url);
                // 创建一个HTTP请求
                request.Method = "GET";
                //返回结果
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader myreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    responseText = myreader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {

                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
            return responseText;
        }
        /// <summary>
        /// Post请求数据
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public string Post(string url, string postData)
        {
            try
            {
                HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
                webrequest.Method = "POST";
                webrequest.ContentType = "application/json;charset=utf-8";
                byte[] postdatabyte = Encoding.UTF8.GetBytes(postData);
                webrequest.ContentLength = postdatabyte.Length;
                Stream stream;
                stream = webrequest.GetRequestStream();
                stream.Write(postdatabyte, 0, postdatabyte.Length);
                stream.Close();
                using (var httpWebResponse = webrequest.GetResponse())
                {
                    using (StreamReader responseStream = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        String ret = responseStream.ReadToEnd();
                        string result = ret.ToString();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return "";
            }
        }
    }
}
