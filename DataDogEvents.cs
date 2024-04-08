using System;
using System.Net;
using System.Text;
using System.IO;
using Microsoft.SqlServer.Server;

public class DataDogEvents
{
    public static void SendEvent(string apiKey, string ddurl, string title, string text, string host, string priority, string tags, string alert_type)
    {

        try
        {
            string datadogUrl = ddurl.ToString();
            string tagsJsonArray = $"[{string.Join(", ", tags.Split(','))}]";
            string requestBody = $"{{\"title\":\"{title}\", \"text\":\"{text}\", \"host\":\"{host}\", \"priority\":\"{priority}\", \"tags\":{tagsJsonArray}, \"alert_type\":\"{alert_type}\"}}";
            string requestUrl = $"{datadogUrl}?api_key={apiKey}";

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
            request.ContentType = "application/json";
            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(requestBody);
                streamWriter.Flush();
                streamWriter.Close();
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                SqlContext.Pipe.Send(result);
            }
        }
        catch (WebException webEx)
        {
            SqlContext.Pipe.Send($"WebException occurred: {webEx.Message}");
            if (webEx.Response != null)
            {
                using (var streamReader = new StreamReader(webEx.Response.GetResponseStream()))
                {
                    string responseText = streamReader.ReadToEnd();
                    SqlContext.Pipe.Send($"Response from server: {responseText}");
                }
            }
        }
        catch (Exception ex)
        {
            SqlContext.Pipe.Send($"An exception occurred: {ex.Message}");
        }
    }
}
