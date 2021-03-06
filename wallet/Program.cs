﻿using utils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace wallet
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Wallet: f=pay from file, l=list, b=balances, p [name]=pay from name");
            Console.SetIn(new StreamReader(Console.OpenStandardInput(18192)));
            var wallet = new Wallet("w");
            int i = 0;
            while (true)
            {
                var line= Console.ReadLine();
                if (line== "f")
                {
                    i++;
                    var f = File.ReadAllLines(@"..\wallet\imp.txt");
                  var addr=  wallet.Import(f[0], f[1]);
                    var coin = Get(addr.pubHash, 0);
                    if (coin==null)
                    {
                        Console.WriteLine("no coin");
                        continue;
                    }
                    var rp = new RequestPay
                    {
                        p = new RequestParent[] { new RequestParent { pubHash = addr.pubHash,publicKey=addr.publicKey, sig = wallet.Sign(coin,addr) } },
                        c = new RequestChild[] {
                        new RequestChild { amount = coin.amount/2, pubHash = wallet.AddKey("alice"+i)},
                        new RequestChild { amount = coin.amount/2, pubHash = wallet.AddKey("bob"+i) }
                     }
                    };

                    ApiUtils.Send(rp,0);

                }
                else if (line == "l")
                {
                    foreach (var a in wallet.addresses)
                    {
                        Console.WriteLine(a.name + ":");
                        Console.WriteLine(a.publicKey);
                        Console.WriteLine(a.privateKey);
                        Console.WriteLine("---");
                    }
                }
                else if (line == "b")
                {
                    for (var j=0; j<10; j++)
                    foreach (var a in wallet.addresses)
                    {
                         var coin = Get(a.pubHash,j);
                        if (coin!=null)
                            Console.WriteLine($"{j}: {a.name} balance: {(coin.amount)}");
                    }
                }
                else if (line.StartsWith("p "))
                {
                    i++;

                    var addr =   wallet.addresses.FirstOrDefault(a=>
                                        a.name==line.Split(' ')[1]);
                    if (addr==null)
                    {
                        Console.WriteLine("no name");
                        continue;
                    }
                    var coin = Get(addr.pubHash,0);
                    if (coin == null)
                    {
                        Console.WriteLine("no coin");
                        continue;
                    }
                    var rp = new RequestPay
                    {
                        p = new RequestParent[] { new RequestParent { pubHash = addr.pubHash,
                            publicKey =addr.publicKey,
                            sig = wallet.Sign(coin,addr) } },
                        c = new RequestChild[] {
                        new RequestChild { amount = coin.amount/2, pubHash = wallet.AddKey("alice"+i)},
                        new RequestChild { amount = coin.amount/2, pubHash = wallet.AddKey("bob"+i) }
                     }
                    };

                    ApiUtils.Send(rp,1);
                }
            }
        }

        private static Coin Get(string pub,int i)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var httpContent = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:809{i}/get");

                    httpContent.Content = new StringContent(pub, Encoding.UTF8, "application/json");
                    var response = client.SendAsync(httpContent).Result.Content.ReadAsStringAsync().Result;
                //    Console.WriteLine("Send! response " + response);
                    return JsonConvert.DeserializeObject<Coin>(response);

                }
            } catch { return null; }
        }
    }
}
