using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Text.Json.Serialization;

namespace Network
{
    class Network_sys{
        
        public const string KEY = "aaa";
        private readonly HttpClient client ;
        private string table_id, table_hash;

        public const string domain = "slottools.japaneast.cloudapp.azure.com";
        public string get_Table_id(){
            return table_id;
        }
        public string get_Table_Hash(){
            return table_hash;
        }
        public Network_sys(string table_id, string table_hash){
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
            client = new HttpClient(handler);
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
            var resp = await ns.post_method<TableResp>(table,"/update_probability");
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
        //現在の「金」を入れてほしい（球数ではない　計算式知らんし） 
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
        //ログアウトのリクエストを送る
        //最初の一回だけ送って
        public async Task<bool> logout_request(){
             var resp = await ns.post_method<UserResult>(user, "/token_exists");
            if("success".Equals(resp.Result)){
                return true;
            }else{
                throw new Exception("アップデートエラー、管理者に問い合わせてください");
            }
        }


        //ログアウトリクエストが承認されたかを確かめるメソッド
        //間違えても↑送んないでね★
        public async Task<bool> logout_isdone(){
             var resp = await ns.post_method<UserResult>(user, "/user_Logout");
            if("success".Equals(resp.Result)){
                return "Logout successful".Equals(resp.Message);
            }else{
                throw new Exception("アップデートエラー、管理者に問い合わせてください");
            }
        }
        public class Result_resp
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("result")]
            public string? Result { get; set; }
        }

        //入金リクエスト
        //↑と同じく、最初の一回だけ送って
        public async Task<bool> request_payment(){
            var req = new {
                username = user.Username,
                table_id = user.Table
            };
            var resp = await ns.post_method<Result_resp>(req, "/request-payment-handle");
            return resp.Success;
        }

        public async Task<bool> request_payment_exists(){
            var req = new {
                username = user.Username,
                table_id = user.Table
            };
            var resp = await ns.post_method<Result_resp>(req, "/request-payment-exists");
            return resp.Success;
        }

        public async Task<bool> Bonus_result(int Money){
            var bonus = new {
                money = Money,
                location = ns.get_Table_id()  
            };
            var resp = await ns.post_method<Result_resp>(bonus, "/Bonus_result_append");
            return resp.Success;
        }
    }


    //作れたら作る
    class Log_Sender{
        public class SendLog
        {
            [JsonPropertyName("level")]
            public int Level { get; set; }
        
            [JsonPropertyName("location")]
            public string Location { get; set; }
        
            [JsonPropertyName("message")]
            public string Message { get; set; }
        }
        private Network_sys net;
        private Queue<SendLog> queue;
        private Timer timer;
        
        private  readonly HttpClient client;

        private async void Execute(object state){
            Console.Write("よびだされ");
            if (queue.Count != 0){
                StringBuilder result = new StringBuilder("[");
                SendLog s;
                while (queue.Count != 0)
                {
                    s = queue.Dequeue();
                    result.Append(JsonSerializer.Serialize(s)).Append(',');
                }
                result.Remove(result.Length - 1, 1).Append("]");
                Console.WriteLine(result.ToString());
                var jsonContent = new StringContent(result.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("https://"+ Network_sys.domain + "/api/add_log_file", jsonContent);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                if("ALL DONE".Equals(jsonResponse)){
                    Console.Write("書き込み完了");
                }else Console.Write("鰓");
            }
        } 
        public Log_Sender(Network_sys ns){
            
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

            client = new HttpClient(handler);
            this.net = ns;
            this.queue = new Queue<SendLog>();
            timer = new System.Threading.Timer(Execute, null, 0, 1000);
        }
        public void log_print(int level, string message){
            var log = new SendLog(){
                Level = level,
                Location = net.get_Table_id(),
                Message = message
            };
            queue.Enqueue(log);
        }
        public void Dispose(){
            timer.Dispose();
        }
        
    }

    // See https://aka.ms/new-console-template for more information
    internal class Myprogram{
        static void Main(String[] args){
            
            var ns = new Network_sys("table_120","21382800000aasssbb");
            var ls = new Log_Sender(ns);
            for(int i = 0; i < 3; i++){
                ls.log_print(i, "Hello World");
            }
            Console.WriteLine("エンターで終了");
            Console.ReadLine();
        }
    }
}