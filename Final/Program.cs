using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using SpotifyAPI.Web.Auth;
using System.IO.Compression;
using System.Windows.Forms;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Download
{
    //Going off spotify playlists

    class download
    {
        public List<string> songName = new List<string>();
        public List<string> dlLink = new List<string>();
        //public List<string> sDiff = new List<string>();
    }
    class Program
    {
        private static int succeded = 0;
        private static int failed = 0;
        private static SpotifyWebAPI _spotify;
        [STAThread]
        static void Main(string[] args) => new Program().startAsync().GetAwaiter().GetResult();
        private async Task startAsync()
        {
            //get users path, dont ask why its up here >:(
            string path = await getPathAsync();
            //Collects the users desired playlist
            List<string> songName = await startSpotifySec();

            //Asks the users what type of search they want
            Console.WriteLine("Select search mode\n1: Top Result\n2: Let you chose for all\n3: Get Highest Rated");
            int userChoice = Convert.ToInt32(Console.ReadLine());

            //loops through every song and downloads it based on users search choice
            foreach (var item in songName)
            {
                HtmlDocument doc = new HtmlDocument();
                
                await getSongAsync(await getDocAsync(doc, $"https://bsaber.com/?s={item}"), userChoice, path);
            }

            Console.WriteLine("Songs Successfully downloaded: {0}\nSong Failed To Download: {1}\nPress Enter To Finish", succeded, failed);
            Console.ReadLine();
        }
        private static async Task<string> getPathAsync()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowDialog();
                return await Task.FromResult(fbd.SelectedPath);
            }
        }

        private async Task<HtmlDocument> getDocAsync(HtmlDocument doc, string URI)
        {           
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent: Other");
                doc.LoadHtml(await client.DownloadStringTaskAsync(URI));
            }

            return doc;
        }

        private async Task getSongAsync(HtmlDocument doc, int userChoice, string path)
        {
            bool failedTofind = false;
            download dl = new download();

            string fileName = "";

            try
            {
                foreach (var item in doc.DocumentNode.SelectNodes("//a[contains(@class, 'action post-icon bsaber-tooltip -download-zip')]"))
                {
                    if (userChoice == 1)
                    {

                        dl.dlLink.Add(item.Attributes[1].Value);
                        //if (dl.sDiff.Count == 0)
                        //{
                        //    foreach (var item2 in doc.DocumentNode.SelectNodes("//a[contains(@class, 'post-difficulty')]"))
                        //    {
                        //        dl.sDiff.Add(item2.Attributes[1].Value.Replace("/songs/?differculty=", ""));
                        //    }
                        //}
                        succeded = succeded + 1;

                        break;
                    }
                    else
                    {
                        //Current Working Position
                        dl.dlLink.Add(item.Attributes[1].Value);
                    }
                }
            }
            catch (Exception)
            {
                failed = failed + 1;
                failedTofind = true;
            }


            int choice = 0;
            if (userChoice == 2)
            {
                try
                {
                    foreach (var item in doc.DocumentNode.SelectNodes("//h4[contains(@class, 'entry-title')]"))
                    {
                        dl.songName.Add(item.ChildNodes[1].Attributes[1].Value);
                        //foreach (var item2 in doc.DocumentNode.SelectNodes("//a[contains(@class, 'post-difficulty')]"))
                        //{
                        //    var temp = item2.Attributes[1].Value.Remove(0,19);
                        //    temp += identifier.ToString();
                        //    dl.sDiff.Add(temp);
                        //}
                        //identifier = identifier + 1;
                    }

                    choice = songChoice(dl);

                    succeded = succeded + 1;
                }
                catch (Exception)
                {
                    failed = failed + 1;
                }
                
            }
            else if (userChoice == 3)
            { 
                try
                {
                    decimal lowest = 10000;
                    string pos = "";
                    string neg = "";

                    //Index, pos, neg, ratio
                    List<Tuple<int, int, int, decimal>> SongRatios = new List<Tuple<int, int, int, decimal>>();
                    //Song Download Link,Song Name, Ratio
                    List<Tuple<string, string, decimal>> SafeArea = new List<Tuple<string, string, decimal>>();

                    //collecting songs
                    HtmlNodeCollection Songs = doc.DocumentNode.SelectNodes("//div[@class='small-12 medium-8 columns' and count(./div[@class='post-row']) > 2 and //a[contains(@class, 'action post-icon bsaber-tooltip -download-zip')]]");

                    //So the sum of the indexes is, (i*2)+1
                    for (int i = 0; i < Songs.Count; i++)
                    {
                        neg = doc.DocumentNode.SelectNodes("//span[@class='post-stat']")[i * 2 + 1].InnerText.Trim();
                        pos = doc.DocumentNode.SelectNodes("//span[@class='post-stat']")[(i * 2)].InnerText.Trim();

                        if (string.IsNullOrEmpty(neg) || string.IsNullOrEmpty(neg))
                        {
                            continue;
                        }

                        if (neg == "0")
                        {
                            neg = "1";
                        }

                        if (pos == "0")
                        {
                            pos = "1";
                        }
                        SongRatios.Add(new Tuple<int, int, int, decimal>(i, int.Parse(pos), int.Parse(neg), decimal.Divide(decimal.Parse(neg),decimal.Parse(pos))));
                    }

                    //next compare and eliminate
                    for (int i = 0; i < Songs.Count; i++)
                    {
                        if (SongRatios[i].Item4 < Convert.ToDecimal(0.5))
                        {
                            //Add to safe area
                            SafeArea.Add(new Tuple<string, string, decimal>(Songs[SongRatios[i].Item1].SelectSingleNode("//a[contains(@class, 'action post-icon bsaber-tooltip -download-zip')]").Attributes[1].Value, Songs[SongRatios[i].Item1].ChildNodes[3].ChildNodes[1].ChildNodes[1].Attributes[1].Value, SongRatios[i].Item4));
                        }
                        else
                        {
                            continue;
                        }

                    }

                    int index = 0;

                    for (int i = 0; i < SafeArea.Count; i++)
                    {
                        if (SafeArea[i].Item3 < lowest)
                        {
                            lowest = SafeArea[i].Item3;
                            index = i;
                        }
                    }

                    succeded = succeded + 1;


                    dl.dlLink.Add(SafeArea[index].Item1);
                }
                catch (Exception)
                {
                    failed = failed + 1;
                }


            }
            if (failedTofind != true)
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent: Other");
                    try
                    {
                        fileName = System.IO.Path.GetRandomFileName();
                        client.DownloadFile(dl.dlLink[choice], $"{path}\\{fileName}.zip");
                    }
                    catch
                    {
                        fileName = System.IO.Path.GetRandomFileName();
                        client.DownloadFile(dl.dlLink[choice], $"{path}\\{fileName}.zip");
                    }
                }

                await unZip(path, fileName);

                System.IO.File.Delete($"{path}\\{fileName}.zip");
            }

        }

        internal async static Task unZip(string path, string songName)
        {
            ZipFile.ExtractToDirectory($"{path}\\{songName}.zip", $"{path}\\{songName}");
        }

        private int songChoice(download dl)
        {
            Console.Clear();
            int i = 0;
            foreach (var item in dl.songName)
            {
                Console.WriteLine($"{i}: {item}");
                i = i + 1;
            }

            Console.WriteLine("Enter the number of the song you want!");
            return Convert.ToInt32(Console.ReadLine());
        }
        private async Task<List<string>> startSpotifySec()
        {
            CredentialsAuth auth = new CredentialsAuth("ca29684d266f4c66b4aa684c0784196a", "20d093a8bec14c5eb2e75067882d1221");
            Token token = await auth.GetToken();
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };

            Console.WriteLine("Pick an option\n1: Album\n2: Playlist\n3: Song");
            int pChoice = Convert.ToInt32(Console.ReadLine());
            switch (pChoice)
            {
                case 1:
                    return await getAlbumAsync(_spotify);
                case 2:
                    return await getPlaylistAsync(_spotify);
                case 3:
                    return await getSongAsync(_spotify);
            }
            return null;
        }

        private async Task<List<string>> getAlbumAsync(SpotifyWebAPI spotify)
        {
            List<string> songNames = new List<string>();
            int offset = 0;
            int i = 0;
            int tracksAmount = 0;

            Console.WriteLine("Enter album id! e.g. https://open.spotify.com/album/6ApYSpXF8GxZAgBTHDzYge the part after /album/");
            string pChoice = Console.ReadLine();

            while (i == tracksAmount && Math.Round((double)i) == offset)
            {
                Paging<SimpleTrack> tracks = await _spotify.GetAlbumTracksAsync(pChoice, limit: 20, offset: offset);
                tracksAmount += tracks.Items.Count;
                foreach (var item in tracks.Items)
                {
                    songNames.Add(item.Name);
                    i++;
                }

                offset = offset + 20;
            }

            return songNames;
        }

        //https://open.spotify.com/track/5w6B0sAH7XauCvMOAtplQj
        private async Task<List<string>> getSongAsync(SpotifyWebAPI spotify)
        {
            List<string> songName = new List<string>();

            Console.WriteLine("Enter song id! e.g. https://open.spotify.com/track/5w6B0sAH7XauCvMOAtplQj the part after /track/");
            string pChoice = Console.ReadLine();

            FullTrack track = await _spotify.GetTrackAsync(pChoice);

            songName.Add(track.Name);
            return songName;
        }

        private async Task<List<string>> getPlaylistAsync(SpotifyWebAPI _spotify)
        {
            List<string> songNames = new List<string>();
            int offset = 0;
            int i = 0;
            int tracksAmount = 0;

            Console.WriteLine("Enter playlist id! e.g. https://open.spotify.com/playlist/0OiX2kYc0DjI3Oc0uqSI5H the part after /playlist/");
            string pChoice = Console.ReadLine();

            while (i == tracksAmount && Math.Round((double)i) == offset)
            {
                Paging<PlaylistTrack> tracks = await _spotify.GetPlaylistTracksAsync(pChoice, limit: 100, offset: offset);
                tracksAmount += tracks.Items.Count;
                foreach (var item in tracks.Items)
                {
                    songNames.Add(item.Track.Name);
                    i++;
                }

                offset = offset + 100;
            }
            return songNames;
        }
    }
}
