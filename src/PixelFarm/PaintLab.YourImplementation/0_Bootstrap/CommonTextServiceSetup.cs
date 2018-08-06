﻿//MIT, 2017-present, WinterDev

using System.IO;
using System.Collections.Generic;
using Typography.FontManagement;

namespace YourImplementation
{
    //class MyIcuDataProvider
    //{
    //    public string icuDir;

    //    public Stream GetDataStream(string strmUrl)
    //    {
    //        string fullname = icuDir + "/" + strmUrl;
    //        //temp fix
    //        if (File.Exists(fullname))
    //        {
    //            return new FileStream(fullname, FileMode.Open);
    //        }

    //        if (PixelFarm.Platforms.StorageService.Provider.DataExists(fullname))
    //        {
    //            return PixelFarm.Platforms.StorageService.Provider.ReadDataStream(fullname);
    //        }
    //        return null;
    //    }
    //}



    public static class CommonTextServiceSetup
    {
        static bool s_isInit;
        //static MyIcuDataProvider s_icuDataProvider;
        static Typography.FontManagement.InstalledTypefaceCollection s_intalledTypefaces;

        static LocalFileStorageProvider s_localFileStorageProvider;
        static FileDBStorageProvider s_filedb;

        public static IInstalledTypefaceProvider FontLoader
        {
            get
            {
                return s_intalledTypefaces;
            }
        }
        public static void SetupDefaultValues()
        {
            //--------
            //This is optional if you don't use Typography Text Service.            
            //-------- 
            if (s_isInit)
            {
                return;
            }

            s_isInit = true;
            s_intalledTypefaces = new InstalledTypefaceCollection();
            s_intalledTypefaces.SetFontNameDuplicatedHandler((existing, newone) =>
            {
                return FontNameDuplicatedDecision.Skip;
            });
            s_intalledTypefaces.LoadSystemFonts();
            s_intalledTypefaces.SetFontNotFoundHandler((collection, fontName, subFam) =>
            {
                //This is application specific ***
                //
                switch (fontName.ToUpper())
                {
                    default:
                        {

                        }
                        break;
                    case "SANS-SERIF":
                        {
                            //temp fix
                            InstalledTypeface ss = collection.GetInstalledTypeface("Microsoft Sans Serif", "REGULAR");
                            if (ss != null)
                            {
                                return ss;
                            }
                        }
                        break;
                    case "SERIF":
                        {
                            //temp fix
                            InstalledTypeface ss = collection.GetInstalledTypeface("Palatino linotype", "REGULAR");
                            if (ss != null)
                            {
                                return ss;
                            }
                        }
                        break;
                    case "TAHOMA":
                        {
                            switch (subFam)
                            {
                                case "ITALIC":
                                    {
                                        InstalledTypeface anotherCandidate = collection.GetInstalledTypeface(fontName, "NORMAL");
                                        if (anotherCandidate != null)
                                        {
                                            return anotherCandidate;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "MONOSPACE":
                        //use Courier New
                        return collection.GetInstalledTypeface("Courier New", subFam);
                    case "HELVETICA":
                        return collection.GetInstalledTypeface("Arial", subFam);
                }
                return null;
            });
            //--------------------
            InstalledTypefaceCollection.SetAsSharedTypefaceCollection(s_intalledTypefaces);

            //1. Storage provider
            s_localFileStorageProvider = new LocalFileStorageProvider();
#if DEBUG
            // choose local file or filedb 
            // if we choose filedb => then this will create/open a 'disk' file for read/write data
            s_filedb = new FileDBStorageProvider("textservicedb");
            // then register to the storage service
            PixelFarm.Platforms.StorageService.RegisterProvider(s_filedb);

            //--------
            //Typography's TextServices
            //you can implement   Typography.TextBreak.DictionaryProvider  by your own

            //this set some essentail values for Typography Text Serice
            // 
            //2.2 Icu Text Break info
            //test Typography's custom text break,
            //check if we have that data?             
            string typographyDir = @"d:/test/icu60/brkitr_src/dictionaries";
            if (!System.IO.Directory.Exists(typographyDir))
            {
                throw new System.NotSupportedException("dic");
            }

            var dicProvider = new IcuSimpleTextFileDictionaryProvider() { DataDir = typographyDir };
            Typography.TextBreak.CustomBreakerBuilder.Setup(dicProvider);
#endif
        }


        class IcuSimpleTextFileDictionaryProvider : Typography.TextBreak.DictionaryProvider
        {
            //read from original ICU's dictionary
            //.. 
            public string DataDir
            {
                get;
                set;
            }
            public override IEnumerable<string> GetSortedUniqueWordList(string dicName)
            {
                //user can provide their own data 
                //....

                switch (dicName)
                {
                    default:
                        return null;
                    case "thai":
                        return GetTextListIterFromTextFile(DataDir + "/thaidict.txt");
                    case "lao":
                        return GetTextListIterFromTextFile(DataDir + "/laodict.txt");
                }

            }
            static IEnumerable<string> GetTextListIterFromTextFile(string filename)
            {
                //read from original ICU's dictionary
                //..

                using (FileStream fs = new FileStream(filename, FileMode.Open))
                using (StreamReader reader = new StreamReader(fs))
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        line = line.Trim();
                        if (line.Length > 0 && (line[0] != '#')) //not a comment
                        {
                            yield return line.Trim();
                        }
                        line = reader.ReadLine();//next line
                    }
                }
            }
        }
    }


}