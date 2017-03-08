using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace tayak2
{

    struct myStruct
    {
        public int Ns, Nf;
        public char C;
        public bool Isf;
    }

    class fsm
    {
        List<myStruct> stateList = new List<myStruct>();
        List<myStruct> detStateList = new List<myStruct>();
        Dictionary<int, List<myStruct>> groups = new Dictionary<int, List<myStruct>>();
        List<int> ng = new List<int>();
        bool isdet = true;
        bool opdet = false;
        private int max = 0;

        public void Read(string fileName)
        {
            stateList.Clear();
            StreamReader read = new StreamReader(fileName);
            string str;
            myStruct state = new myStruct();
            while ((str = read.ReadLine()) != null)
            {
                if (str.Length < 7) continue;
                //if (str[0] != 'q') continue;
                state.Isf = false;
                state.Ns = -1;
                string s = "";
                str += ',';
                bool f = false;
                bool br = false;
                for (int i = 1; i < str.Length; i++)
                {
                    if (f)
                    {
                        state.C = str[i];
                        f = false;
                        continue;
                    }
                    if (str[i] == ',')
                    {
                        try
                        {
                            Convert.ToInt32(s);
                        }
                        catch (Exception)
                        {
                            br = true;
                            break;
                        }
                        if (state.Ns != -1)
                            state.Nf = Convert.ToInt32(s);
                        else
                            state.Ns = Convert.ToInt32(s);
                        s = "";
                        f = true;
                        continue;
                    }
                    if (str[i - 1] == '=' && str[i] == 'f')
                    {
                        state.Isf = true;
                        s = "";
                        continue;
                    }
                    if (str[i - 1] == '=' && str[i] == 'q')
                    {
                        state.Isf = false;
                        s = "";
                        continue;
                    }
                    s += str[i];
                }
                if (br) continue;
                stateList.Add(state);
            }
            read.Close();
            stateList = stateList.OrderBy(x => x.Ns).ThenBy(x => x.C).ToList();
            isDeterm();
            if (!isdet) toDet();
        }

        private void isDeterm()
        {
            for (int i = 1; i < stateList.Count; i++)
            {
                if (stateList[i].Ns == stateList[i - 1].Ns && stateList[i].C == stateList[i - 1].C)
                {
                    isdet = false;
                    Console.WriteLine("Не детерминировано");
                    return;
                }
                isdet = true;
            }
            if (isdet)
            Console.WriteLine("Детерминировано");
        }

        private void toDet()
        {
            opdet = true;
            ng.Clear();
            var groupstateList = stateList.GroupBy(x => new { x.Ns, x.C }).ToList();
            max = stateList.Max(x => x.Ns);
            if (stateList.Max(x => x.Nf) > max) max = stateList.Max(x => x.Ns);
            detStateList.Clear();
            groups.Clear();
            myStruct state = new myStruct();
            foreach (var item in groupstateList)
            {
                var group = item.OrderBy(x => x.Nf).ToList();
                if (group.Count > 1)
                {
                    state.Ns = item.Key.Ns;
                    state.C = item.Key.C;
                    if (groups.Count == 0)
                    {
                        max++;
                        groups.Add(max, group);
                        state.Nf = max;
                        ng.Add(max);
                    }
                    else
                    {
                        // проверяем существование группы
                        var exists = groups.Where(x => Equals(x.Value.Select(y => y.Nf), group.Select(z => z.Nf)) && Equals(x.Value.Select(w => w.Isf), group.Select(q => q.Isf))).ToList();
                        if (exists.Count == 0)
                        {
                            max++;
                            groups.Add(max, group);
                            state.Nf = max;
                            ng.Add(max);
                        }
                        else
                            state.Nf = exists.ElementAt(0).Key;
                    }
                    state.Isf = group.Exists(x => x.Isf);
                }
                else
                    state = group[0];
                detStateList.Add(state);
            }
            bool newgroup = true;
            Dictionary<int, List<myStruct>> newGroups = new Dictionary<int, List<myStruct>>();

            while (newgroup)
            {
                newgroup = false;
                List<myStruct> newDetStateList = new List<myStruct>();
                foreach (var item in groups)
                {
                    state.Ns = item.Key;
                    List<char> sym = new List<char>();
                    foreach (var it in item.Value)
                    {
                        var collection = detStateList.Where(x => x.Ns == it.Nf);
                        List<myStruct> sum = new List<myStruct>();
                        foreach (var varaible in collection)
                        {
                            sum.Clear();
                            if (!sym.Contains(varaible.C))
                            {
                                sym.Add(varaible.C);
                                var ololo = detStateList.Where(x => x.C == varaible.C && item.Value.Where(y => !y.Isf).Select(z => z.Nf).Contains(x.Ns));
                                foreach (var x in ololo.Where(x => !sum.Select(z => new { z.Nf, z.Isf }).Contains(new { x.Nf, x.Isf })))
                                    sum.Add(x);
                                if (sum.Count == 0)
                                    continue;
                                if (sum.Count == 1)
                                {
                                    state.Nf = sum[0].Nf;
                                    state.Isf = sum[0].Isf;
                                    state.C = varaible.C;
                                    newDetStateList.Add(state);
                                }
                                else
                                {
                                    var olo = groups.Select(x => new { x.Key, Value = x.Value.Select(y => new { y.Nf, y.Isf }).ToList() }).ToList();
                                    if (olo.Select(x => x.Value).Contains(sum.Select(x => new { x.Nf, x.Isf })))
                                    {
                                        var key =
                                            olo.Where(x => x.Value == sum.Select(y => new { y.Nf, y.Isf })).ElementAt(0).Key;
                                        state.Nf = key;
                                        state.Isf = groups[key].Exists(x => x.Isf);
                                        state.C = varaible.C;
                                        newDetStateList.Add(state);
                                    }
                                    else
                                    {
                                        // случай с новой группой
                                        newgroup = true;
                                        max++;
                                        newGroups.Add(max, sum);
                                        ng.Add(max);
                                    }
                                }
                            }
                        }
                    }
                }
                newGroups.Add(1, new List<myStruct>());
                detStateList = detStateList.Concat(newDetStateList).ToList();
                groups = newGroups;
                newGroups.Clear();
            }
        }

        public void write()
        {
            if (!opdet)
            {
                foreach (var item in stateList)
                {
                    char c = ' ';
                    if (item.Isf) c = 'f';
                    else c = 'q';
                    Console.WriteLine("q" + item.Ns + "," + item.C + "=" + c + item.Nf);
                }
            }
            else
            {
                Console.WriteLine("Результат:");
                foreach (var item in detStateList)
                {
                    char c = ' ';
                    if (item.Isf) c = 'f';
                    else c = 'q';
                    //if (ng.Contains(item.Nf)) c = 'q';
                    Console.WriteLine("q" + item.Ns + "," + item.C + "=" + c + item.Nf);
                }
            }
        }

        public bool check(string str, int sost, int i)
        {
            bool r = false;
            List<myStruct> statelist;
            if (opdet) statelist = detStateList;
            else
                statelist = stateList;
            var o = statelist.Where(x => x.Ns == sost).ToList();
            if (o.Select(x => x.C).Contains(str[i]))
            {
                foreach (var item in o.Where(x => x.C == str[i]))
                {
                    if (i + 1 > str.Length - 1)
                    {
                        if (item.Isf)
                        {
                            r = true;
                            break;
                        }
                    }
                    else
                    r= check(str, item.Nf, i + 1);
                    if (r) break;
                }
            }
            return r;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var FSM = new fsm();
            FSM.Read("var3_nd.txt");
            FSM.write();
            Console.WriteLine(FSM.check("abf", 0, 0));
            
        }
    }
}
