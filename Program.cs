﻿using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Network
{
    class Network_sys{
        
        public const string KEY = "aaa";
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

    //あったらごめん、それにいい感じにマージして
    class Table{
        public class TableRequest
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }
    
        [JsonPropertyName("table_id")]
        public string? TableId { get; set; }
    
        [JsonPropertyName("probability")]
        public decimal? Probability { get; set; }
    
        [JsonPropertyName("table_hash")]
        public string? TableHash { get; set; }
    }
    
        public class TableResp
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }
    
        [JsonPropertyName("table_id")]
        public string? TableId { get; set; }
    
        [JsonPropertyName("probability")]
        public int Probability { get; set; }
    
        [JsonPropertyName("table_hash")]
        public string? TableHash { get; set; }
    }
    
        private string TableId { get; set; }
        private decimal Probability { get; set; }
        private string TableHash { get; set; }
        
        private Network_sys ns;
        private static async Task<int> get_probability(string TableId, string TableHash, Network_sys ns){
            var table = new TableRequest(){
                Key = Network_sys.KEY,
                TableId = TableId,
                TableHash = TableHash,
                Probability = 1
            };
            var resp = await ns.post_method<TableResp>(table,"/table_probability");
            if("success".Equals(resp.Result)){
                return resp.Probability;
            }else {
                throw new Exception("しらん　なんでこれ出てるん?");
            }
        }
        public static async Task<Table> getTable(string TableId, string TableHash, Network_sys ns){
            return new Table(TableId, TableHash, await get_probability(TableId, TableHash, ns),ns);
        }
        private Table(string TableId, string TableHash, decimal Probability, Network_sys ns){
            this.TableId = TableId;
            this.TableHash = TableHash;
            this.Probability = Probability;
            this.ns = ns;
        }
        //変更するときに呼び出すやつ
        //いらんかもわからん
        public async Task<bool> update_probability(int update){
            var table = new TableRequest(){
                Key = Network_sys.KEY,
                TableId = this.TableId,
                TableHash = this.TableHash,
                Probability = update
            };
            var resp = await ns.post_method<TableResp>(table,"/table_probability");
            if("Success".Equals(resp.Result)){
                return true;
            }
            return false;
        }
        public async Task<int> table_probability(){
            return await get_probability(TableId, TableHash, ns);            
        }
    }

    class Account_SYS{
        public class UserResult
        {
            [JsonPropertyName("result")]
            public string? Result { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("password")]
            public string? Password { get; set; }

            [JsonPropertyName("token")]
            public string? Token { get; set; }

            [JsonPropertyName("table")]
            public string? Table { get; set; }

            [JsonPropertyName("money")]
            public int? Money { get; set; }
        }

        public class UserAuth
        {
            [JsonPropertyName("key")]
            public string? Key { get; set; }

            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("password")]
            public string? Password { get; set; }

            [JsonPropertyName("token")]
            public string? Token { get; set; }

            [JsonPropertyName("table")]
            public string? Table { get; set; }

            [JsonPropertyName("money")]
            public decimal? Money { get; set; }
        }

        private UserAuth user;
        private Network_sys ns;

        //ゲスト用
        public static async Task<Account_SYS> getAccount_SYS(Network_sys ns){
            var userauth = new UserAuth(){
              Key = Network_sys.KEY,
              Username = "",
              Password = "",
              Token = "",
              Table = ns.get_Table_id(),
              Money = 0 
            };
            var resp = await ns.post_method<UserResult>(userauth, "/create_guest_user");
            var newAccount_sys = new Account_SYS(resp.Token, resp.Username, resp.Password,resp.Table,ns);
            return newAccount_sys;
        }

        //ユーザー用
        public static async Task<Account_SYS> getAccount_SYS(string? username,string? password,Network_sys ns){
            var userauth = new UserAuth(){
              Key = Network_sys.KEY,
              Username = username,
              Password = password,
              Token = "",
              Table = ns.get_Table_id(),
              Money = 0 
            };
            var resp = await ns.post_method<UserResult>(userauth, "/user_Login");
            var newAccount_sys = new Account_SYS(resp.Token, resp.Username, resp.Password,resp.Table,ns);
            return newAccount_sys;
        }
        
        
        public Account_SYS(string? TOKEN,string? username, string? password,string? table_id,Network_sys ns){
            user = new UserAuth(){
                Key = "aaa",
                Username = username,
                Password = password,
                Token = TOKEN,
                Table = table_id,
                Money = 0
            };
            this.ns = ns;
        }
        //現在の「金」を入れてほしい（球数じゃなくて　計算式知らんし） 
        public async Task<bool> update_money(int money){
            var tmp = user.Money;
            user.Money = money;
            var resp = await ns.post_method<UserResult>(user, "/update_money");
            if("success".Equals(resp.Result)){
                return true;
            }else{
                user.Money = tmp;
                throw new Exception("アップデートエラー、管理者に問い合わせてください");
            }
        }
        //鯖に登録されてる「金」の状況確認
        public async Task<int> get_user_money(){
            var resp = await ns.post_method<UserResult>(user, "/get_user_money");
            if("success".Equals(resp.Result) && resp.Money != null){
                return (int)resp.Money;
            }else{
                throw new Exception("アップデートエラー、管理者に問い合わせてください");
            }
        }
    }


    //作れたら作る
    class Log_Sender{

    }

    // See https://aka.ms/new-console-template for more information
    internal class Myprogram{
        static void Main(String[] args){ 
            Console.WriteLine("Hello, World!");
        }
    }
}