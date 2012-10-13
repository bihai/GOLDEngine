using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

using GOLDEngine;

namespace TestGOLDEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            // Use the definition of the GOLD meta-language, to test itself.
            test("Tests\\GOLD Meta-Language (2.6.0).egt", "Tests\\GOLD Meta-Language (2.6.0).grm");
        }

        static void assert(bool b)
        {
            if (!b)
                throw new ApplicationException();
        }

        static void test(string grammarFile, string contentFile)
        {
            WrapGOLDEngine driver = new WrapGOLDEngine();
            driver.LoadTables(grammarFile);
            Reduction reduction = driver.ParseFile(contentFile);
            assert(reduction != null);
        }
    }
}
