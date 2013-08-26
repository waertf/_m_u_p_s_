using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;        // for XmlTextReader and XmlValidatingReader
using System.Xml.Schema; // for XmlSchemaCollection (which is used later)


namespace ConsoleApplication1_test_xml_dtd
{
    class Program
    {
        private static bool isValid = true;      // If a validation error occurs,
        // set this flag to false in the
        // validation event handler.

        static void Main(string[] args)
        {
            XmlTextReader r = new XmlTextReader("Location-Registration-Request.xml");
            XmlValidatingReader v = new XmlValidatingReader(r);
            v.ValidationType = ValidationType.DTD;
            v.ValidationEventHandler += new ValidationEventHandler(v_ValidationEventHandler);
            while (v.Read())
            {
                // Can add code here to process the content.
            }
            v.Close();

            // Check whether the document is valid or invalid.
            if (isValid)
                Console.WriteLine("Document is valid");
            else
                Console.WriteLine("Document is invalid");
            Console.WriteLine();

        }

        static void v_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            isValid = false;
            Console.WriteLine("Validation event\n" + e.Message);

        }
    }
}
