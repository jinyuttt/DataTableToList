using DataTableToList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Person person = new Person();
            person.Age = 3;
            person.ID = 1;
            person.Note = "";
            int ss=Convert.ToInt32(person.Age);

            DataTable dt = new DataTable();
            Random random = new Random();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("PersonName", typeof(string));
            dt.Columns.Add("Age", typeof(int));
            for (int i = 1; i < 10000000; i++)
            {
                var row = dt.NewRow();
                row[0] = i;
                row[1] = "jy" + random.Next();
                row[2] = random.Next(10, 50);
                dt.Rows.Add(row);
            }
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            List<Person> lst = dt.ToEntityList<Person>();
            watch.Stop();
            Console.WriteLine(lst.Count + "," + watch.ElapsedMilliseconds);
            while (true)
            {
                watch.Restart();
                lst = dt.ToEntityList<Person>();
                //Stopwatch watchDD = new Stopwatch();
                //watchDD.Start();
                watch.Stop();
                Console.WriteLine(lst.Count + "," + watch.ElapsedMilliseconds);
            }
            Console.ReadKey();
        }
    }
}
