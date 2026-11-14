using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace NiuMa
{
    public class AppSettings
    {
        public string AppName = "燃脂助手";
        public int SalaryMode = 0;          // 0 每日, 1 每月
        public double Amount = 100;
        public double MonthDays = 21.75;
        public string StartTime = "10:00";
        public string EndTime = "19:00";
        public bool LunchEnabled = false;
        public string LunchStart = "12:00";
        public string LunchEnd = "13:00";
        public string WorkDays = "1,2,3,4,5"; // 0=周日
        public int Decimals = 5;
        public string Currency = "卡路里";
        public double BgOpacity = 1.0;
        public double PanelOpacity = 0.55;
        public double WindowOpacity = 1.0;
        public double UiZoom = 1.0;         // 在「按屏幕自动」的基础上再放大/缩小
        public bool Topmost = true;
        public bool ClickThrough = false;
        public bool AutoHide = true;        // 贴到屏幕左右边缘后自动收纳
        public int DockSide = 0;            // 0 未贴边, 1 贴左, 2 贴右
        public bool Mini = false;
        public bool Masked = false;
        public bool Odometer = true;
        public bool Overtime = false;
        public double OvertimeRate = 1.5;
        public int PayDay = 10;
        public bool OffWorkNotify = true;
        public string BgPath = "";
        public double Left = double.NaN;
        public double Top = double.NaN;
        public string OverrideDate = "";
        public int OverrideMode = 0;        // 0 正常, 1 今天休息, 2 今天照常
        public string Wishlist = "一杯美式=3\n一个苹果=8\n一份沙拉=15\n一节拉伸=22\n一次慢跑=30\n一场球=45\n一次游泳=79\n一节私教=120\n一双跑鞋=399";
        public string Quotes = "卡路里不会说谎，烧掉就是烧掉。\n每一秒都在消耗，别停。\n今天多燃一点，明天就轻一点。\n数字在涨，说明你没闲着。\n燃烧这件事，会上瘾。\n慢没关系，别断。\n每一克，都在被数字说服。\n今天的额度，用满才算完整。\n不动的时候，数字也停着。\n再坚持一会儿，就到收尾了。";

        public static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NiuMaTuoMinQi"); }
        }
        public static string FilePath { get { return Path.Combine(Dir, "settings.ini"); } }

        public AppSettings Clone() { return (AppSettings)MemberwiseClone(); }

        public double DailyPay()
        {
            if (SalaryMode == 1) return MonthDays > 0 ? Amount / MonthDays : 0;
            return Amount;
        }

        public bool[] WorkDaySet()
        {
            var set = new bool[7];
            foreach (var p in (WorkDays ?? "").Split(','))
            {
                int d;
                if (int.TryParse(p.Trim(), out d) && d >= 0 && d < 7) set[d] = true;
            }
            return set;
        }

        public static bool TryParseTime(string s, out TimeSpan t)
        {
            t = TimeSpan.Zero;
            if (s == null) return false;
            var parts = s.Trim().Replace('：', ':').Split(':');
            int h, m;
            if (parts.Length != 2 || !int.TryParse(parts[0], out h) || !int.TryParse(parts[1], out m)) return false;
            if (h < 0 || h > 23 || m < 0 || m > 59) return false;
            t = new TimeSpan(h, m, 0);
            return true;
        }

        public static TimeSpan Time(string s, TimeSpan fallback)
        {
            TimeSpan t;
            return TryParseTime(s, out t) ? t : fallback;
        }

        public List<KeyValuePair<string, double>> WishItems()
        {
            var list = new List<KeyValuePair<string, double>>();
            foreach (var line in (Wishlist ?? "").Split('\n'))
            {
                var l = line.Trim().Replace('＝', '=');
                int i = l.LastIndexOf('=');
                double price;
                if (i <= 0) continue;
                if (double.TryParse(l.Substring(i + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out price) && price > 0)
                    list.Add(new KeyValuePair<string, double>(l.Substring(0, i).Trim(), price));
            }
            list.Sort((a, b) => a.Value.CompareTo(b.Value));
            return list;
        }

        public string[] QuoteList()
        {
            var list = new List<string>();
            foreach (var q in (Quotes ?? "").Split('\n')) if (q.Trim().Length > 0) list.Add(q.Trim());
            return list.ToArray();
        }

        static string Esc(string s) { return s.Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n"); }
        static string Unesc(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length) { i++; sb.Append(s[i] == 'n' ? '\n' : s[i]); }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var sb = new StringBuilder();
                foreach (var f in typeof(AppSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object v = f.GetValue(this);
                    string s = v is double ? ((double)v).ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(v, CultureInfo.InvariantCulture);
                    sb.Append(f.Name).Append('=').Append(Esc(s ?? "")).Append("\r\n");
                }
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static AppSettings Load()
        {
            var s = new AppSettings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    var f = typeof(AppSettings).GetField(line.Substring(0, i), BindingFlags.Public | BindingFlags.Instance);
                    if (f == null) continue;
                    string v = Unesc(line.Substring(i + 1));
                    try
                    {
                        if (f.FieldType == typeof(string)) f.SetValue(s, v);
                        else if (f.FieldType == typeof(int)) f.SetValue(s, int.Parse(v, CultureInfo.InvariantCulture));
                        else if (f.FieldType == typeof(double)) f.SetValue(s, double.Parse(v, CultureInfo.InvariantCulture));
                        else if (f.FieldType == typeof(bool)) f.SetValue(s, bool.Parse(v));
                    }
                    catch { }
                }
            }
            catch { }
            return s;
        }
    }
}
