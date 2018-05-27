using GooglePlaceAPIKeys.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Net;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static GooglePlaceAPIKeys.Models.GooglePlaceAPIResponse;
using RestSharp;
using System.Threading.Tasks;

namespace GooglePlaceAPIKeys.Controllers
{
    [System.Web.Http.RoutePrefix("api/Keys")]
    public class KeysController : ApiController
    {

        string baseURI = String.Empty;
        List<string> keys;
        Dictionary<string,List<string>> addrsstypes;

        string status = string.Empty;
        public KeysController()
        {
            DateTime thisDay = DateTime.Today;
            keys = new List<string>();
            addrsstypes = new Dictionary<string, List<string>>();
            baseURI = "https://maps.googleapis.com/maps/api/place/textsearch/json?query=";
           
        }
        [System.Web.Http.HttpGet]
        public string Get()
        {
            return "Hi !There ";
                }
        // GET: Keys
        [System.Web.Http.HttpPost]
        public Dictionary<string, List<string>> GetPlaceDetail(Address address)
        {
            string connectString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\inetpub\wwwroot\GooglePlaceAPIKeys\App_Data\Keys.mdf; Integrated Security=True";
            try
            {

                SqlConnection con = new SqlConnection();
                con.ConnectionString = connectString;

                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "select * from [Key]";
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    keys.Add(reader.GetValue(0).ToString());
                }
                reader.Close();
                con.Close();
                //int j = 0;
                int i = 0;
                Parallel.For(0, address.addresses.Count,
                j =>
                {
                    var myi = i;
                    var result = string.Empty;
                    if (!(i >= keys.Count))
                    {
                        string uri = baseURI + address.addresses[j] + "&key=" + keys[myi];

                        HttpWebRequest webrequest =
                        (HttpWebRequest)WebRequest.Create(uri);
                        webrequest.ContentType = "application/json";
                        //webrequest.KeepAlive = false;
                        //webrequest.Method = "GET";
                        //webrequest.Accept = "application/json";
                        //webrequest.ContentType = "application/json";
                        using (var response = webrequest.GetResponse())
                        using (var stream = response.GetResponseStream())
                        using (var strmReader = new System.IO.StreamReader(stream))
                        {
                            result = strmReader.ReadToEnd();
                        }

                        GooglePlaceAPIResponse jsondata = (GooglePlaceAPIResponse)JsonConvert.DeserializeObject<GooglePlaceAPIResponse>(result);

                        if (jsondata.status.ToUpper() == "OK")
                        {
                            using (SqlConnection con1 = new SqlConnection(connectString))
                            {
                                con1.Open();
                                SqlCommand cmd1 = new SqlCommand();
                                cmd1.Connection = con1;

                                cmd1.CommandText = "update [dbo].[Table] set KeyCounter=KeyCounter + 1 where ApiKey = '" + keys[myi] + "'";
                                cmd1.ExecuteNonQuery();

                                con1.Close();
                            }
                            List<string> types = jsondata.results[0].types;
                            //   List<string> types = rs.types;
                            addrsstypes.Add(address.addresses[j], types);
                            j++;
                            // JToken jtoken= jsondata["results"]["place_id"]
                        }
                        else if (jsondata.status.ToUpper() == "OVER_QUERY_LIMIT" || jsondata.status.ToUpper() == "REQUEST_DENIED")
                        {
                            i++;
                        }
                        else if (jsondata.status.ToUpper() == "ZERO_RESULTS")
                        {
                            j++;
                        }
                        else
                        {
                            i++;
                        }

                    }
                    else
                    {
                        List<string> emptyStringmsg = null;
                        if (emptyStringmsg != null)
                        {
                            emptyStringmsg.Add("keys have run out. try out again.");
                        }

                        addrsstypes.Add(address.addresses[j], emptyStringmsg);

                    }
                    //var jsondata = JObject.Parse(result);
                    // status = jsondata;
                    // j++;
                });
                con.Close();
            }
            catch (WebException ex)
            {

                throw new HttpException(500, ex.Message);

            }
            return addrsstypes;
        }


    }
}