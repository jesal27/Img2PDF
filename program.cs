using iTextSharp.text;
using iTextSharp.text.pdf;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Work
    {

        public static int mCount=0;

        private int mImageID;

        private static readonly ILog Log = LogManager.GetLogger(typeof(Work));

        private static readonly string mConnStringImages = "Data Source=xxx;Initial Catalog=xxx;uid=xxx;pwd=xxx";

        public Work(int imageID)
        {
            mImageID = imageID;
        }

        public void Process()
        {
            string msg = "Processing: " + mImageID;

            mCount++;

            try
            {

                Log.Info(msg);

                object data = GetImage();

                CreatePDF(data);

                Import();
            }
            catch (Exception ex)
            {
                Log.Error(msg, ex);
            }
        }

        public void CreatePDF(object o)
        {
            try
            {

                //Convert the images to a PDF and save it.

                using (Document document = new Document(PageSize.A4))
                {
                    using (var stream = new FileStream(@"xxx" + mImageID + ".pdf", FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        PdfWriter.GetInstance(document, stream);

                        document.Open();

                        var image = iTextSharp.text.Image.GetInstance((byte[])o);

                        image.ScaleToFit(PageSize.A4);

                        document.Add(image);

                        document.Close();

                    }
                }
            }
            catch
            {
                throw;
            }


        }
        public object GetImage()
        {
            object retVal;

            using (SqlConnection conn = new SqlConnection(mConnStringImages))
            {

                conn.Open();

                SqlCommand comm = new SqlCommand("dbo.GetImage", conn);
                comm.CommandType = CommandType.StoredProcedure;

                comm.Parameters.AddWithValue("@imageID", mImageID);

                retVal = comm.ExecuteScalar();
            }

            return retVal;
        }

        private void Import()
        {

            try
            {
                byte[] data = null;

                using (FileStream fs = new FileStream(@"xxx" + mImageID + ".pdf", FileMode.Open))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        fs.CopyTo(ms);

                        data = ms.ToArray();
                    }
                }

                UploadImage(data);

            }
            catch
            {
                throw;
            }

        }
        public void UploadImage(byte[] image)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(mConnStringImages))
                {
                    conn.Open();

                    SqlCommand comm = new SqlCommand("dbo.UpdateImage2", conn);
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@image", image);
                    comm.Parameters.AddWithValue("@id", mImageID);
                    comm.ExecuteNonQuery();
                }
            }
            catch
            {
                throw;
            }

        }
    }
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        private static List<Int32> mList = new List<int>();
        private static readonly string mConnStringImages = "Data Source=xxx;Initial Catalog=xxx;uid=xxx;pwd=xxx";

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            mList = GetScanDocs();

            Log.Info(DateTime.Now);

            Console.WriteLine("Wait...");

            Parallel.ForEach(mList, x =>
            {
                if(Work.mCount%10000==0)
                    Console.WriteLine(Work.mCount);

                Work obj = new Work(x);

                obj.Process();

            });

            Log.Info(DateTime.Now);

            Console.WriteLine("Done!");

            Console.Read();

        }

        public static List<Int32> GetScanDocs()
        {

            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(mConnStringImages))
            {

                conn.Open();

                SqlCommand comm = new SqlCommand("dbo.GetIDList", conn);
                comm.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                da.Fill(dt);

                return dt.AsEnumerable().Select(x => x.Field<int>("ID")).ToList();
            }
        }


    }
}
