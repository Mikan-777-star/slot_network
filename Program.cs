using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Network
{
    class Network_sys{
        private static readonly HttpClient client = new HttpClient();
        private string table_id, table_hash;

        private const string domain = "slottools.japaneast.cloudapp.azure.com";
        public string get_Table_id(){
            return table_id;
        }
        public string get_Table_Hash(){
            return table_hash;
        }
        public Network_sys(string table_id, string table_hash){
            this.table_hash = table_hash;
            this.table_id   = table_id;
        }
        public async Task<T> post_method<T>(object data, string url){
            var jsonContent = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("https://"+ domain + url, jsonContent);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            
            T? res = JsonSerializer.Deserialize<T>(jsonResponse);
            if (res == null){
                throw new Exception("JSON エラー");
            }
            return res;
        }
    }

    // See https://aka.ms/new-console-template for more information
    internal class Myprogram{
        static void Main(String[] args){ 
            Console.WriteLine("Hello, World!");
        }
    }
}