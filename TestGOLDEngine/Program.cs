using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace TestGOLDEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            firstTest();
        }

        static void assert(bool b)
        {
            if (!b)
                throw new ApplicationException();
        }

        static void firstTest()
        {
            // Use the definition of the GOLD meta-language, to test itself.
            WrapGOLDEngine wrapper;
            using (Stream grammar = new FileStream("Tests\\GOLD Meta-Language (2.6.0).egt", FileMode.Open))
            {
                wrapper = new WrapGOLDEngine(grammar);
            }
            using (TextReader content = new StreamReader("Tests\\GOLD Meta-Language (2.6.0).grm"))
            {
                GOLDEngine.Reduction reduction = wrapper.Parse(content);
                assert(reduction != wrapper.Parse(content));
                //reduction[0].
            }
        }
    }
}
