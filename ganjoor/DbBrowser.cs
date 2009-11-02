﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using System.IO;

namespace ganjoor
{
    class DbBrowser
    {
        #region Constructor
        public DbBrowser()
        {
            try
            {
                SQLiteConnectionStringBuilder conString = new SQLiteConnectionStringBuilder();
                conString.DataSource = "ganjoor.s3db";
                conString.DefaultTimeout = 5000;
                conString.FailIfMissing = true;
                conString.ReadOnly = false;
                _con = new SQLiteConnection(conString.ConnectionString);
                _con.Open();
            }
            catch(Exception exp)
            {
                LastError = exp.ToString();
                _con = null;
            }
            if (_con != null)
            {
                UpgradeOldDbs();
            }
        }
        
        ~DbBrowser()
        {
            if (_con != null)
                _con.Close();
        }
        #endregion

        #region Variables
        private SQLiteConnection _con;
        public string LastError = string.Empty;
        #endregion 

        #region Properties & Methods
        public bool Failed
        {
            get
            {
                return (null == _con);
            }
        }
        public bool Connected
        {
            get
            {
                return (null != _con);
            }
        }
        public List<GanjoorPoet> Poets
        {
            get
            {
                List<GanjoorPoet> poets = new List<GanjoorPoet>();
                if (Connected)
                {
                    using (DataTable tbl = new DataTable())
                    {
                        using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT id, name, cat_id FROM poet", _con))
                        {
                            da.Fill(tbl);
                            foreach (DataRow row in tbl.Rows)
                            {
                                poets.Add(new GanjoorPoet(Convert.ToInt32(row.ItemArray[0]), row.ItemArray[1].ToString(), Convert.ToInt32(row.ItemArray[2])));
                            }
                        }
                    }
                }
                return poets;
            }
        }
        public GanjoorCat GetCategory(int CatID)
        {
            if (Connected)                
            using (DataTable tbl = new DataTable())
            {
                using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT poet_id, text, parent_id, url FROM cat WHERE id = " + CatID.ToString(), _con))
                {
                    da.Fill(tbl);
                    System.Diagnostics.Debug.Assert(tbl.Rows.Count < 2);
                    if (tbl.Rows.Count == 1)
                        return new GanjoorCat(
                            CatID,
                            Convert.ToInt32(tbl.Rows[0].ItemArray[0]),
                            tbl.Rows[0].ItemArray[1].ToString(),
                            Convert.ToInt32(tbl.Rows[0].ItemArray[2]),
                            tbl.Rows[0].ItemArray[3].ToString().ToString()
                            );
                }
            }
            return null;
        }
        public List<GanjoorCat> GetSubCategories(int CatID)
        {
            List<GanjoorCat> lst = new List<GanjoorCat>();
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT poet_id, text, url, ID FROM cat WHERE parent_id = " + CatID.ToString(), _con))
                    {
                        da.Fill(tbl);
                        foreach (DataRow row in tbl.Rows)
                            lst.Add(new GanjoorCat(
                                Convert.ToInt32(row.ItemArray[3]),
                                Convert.ToInt32(row.ItemArray[0]),
                                row.ItemArray[1].ToString(),
                                CatID,
                                row.ItemArray[2].ToString().ToString()
                                ));
                    }
                }
            }
            return lst;
        }
        public List<GanjoorCat> GetParentCategories(GanjoorCat Cat)
        {
            List<GanjoorCat> lst = new List<GanjoorCat>();
            if (Connected)
            {
                while (Cat!=null && Cat._ParentID != 0)
                {
                    Cat = GetCategory(Cat._ParentID);
                    lst.Insert(0, Cat);
                }
                lst.Insert(0,new GanjoorCat(0, 0, "خانه", 0, ""));
            }
            return lst;
        }
        public List<GanjoorPoem> GetPoems(int CatID)
        {
            List<GanjoorPoem> lst = new List<GanjoorPoem>();
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT ID, title, url FROM poem WHERE cat_id = " + CatID.ToString(), _con))
                    {
                        da.Fill(tbl);
                        foreach (DataRow row in tbl.Rows)
                        {
                            int PID = Convert.ToInt32(row.ItemArray[0]);
                            lst.Add(new GanjoorPoem(
                                PID,
                                CatID,
                                row.ItemArray[1].ToString(),
                                row.ItemArray[2].ToString().ToString(),
                                IsPoemFaved(PID)
                                ));
                        }
                    }
                }
            }
            return lst;
        }
        public List<GanjoorVerse> GetVerses(int PoemID)
        {
            return GetVerses(PoemID, 0);
        }
        public List<GanjoorVerse> GetVerses(int PoemID, int Count)
        {
            List<GanjoorVerse> lst = new List<GanjoorVerse>();
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT vorder, position, text FROM verse WHERE poem_id = " + PoemID.ToString() + " order by vorder" + (Count>0 ? " LIMIT "+Count.ToString() : ""), _con))
                    {
                        da.Fill(tbl);
                        foreach (DataRow row in tbl.Rows)
                            lst.Add(new GanjoorVerse(
                                PoemID,                                
                                Convert.ToInt32(row.ItemArray[0]),
                                (VersePosition)Convert.ToInt32(row.ItemArray[1]),
                                row.ItemArray[2].ToString()
                                ));
                    }
                }
            }
            return lst;
        }        
        public GanjoorPoem GetPoem(int PoemID)
        {
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT cat_id, title, url FROM poem WHERE ID = " + PoemID.ToString(), _con))
                    {
                        da.Fill(tbl);
                        System.Diagnostics.Debug.Assert(tbl.Rows.Count < 2);
                        if(1 == tbl.Rows.Count)
                            return
                                new GanjoorPoem(
                                PoemID,
                                Convert.ToInt32(tbl.Rows[0].ItemArray[0]),
                                tbl.Rows[0].ItemArray[1].ToString(),
                                tbl.Rows[0].ItemArray[2].ToString().ToString(),
                                IsPoemFaved(PoemID)
                                );
                    }
                }
            }
            return null;
        }
        public GanjoorPoem GetNextPoem(int PoemID, int CatID)
        {
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(
                        String.Format(
                        "SELECT ID FROM poem WHERE cat_id = {0} AND id>{1} LIMIT 1"
                        ,
                        CatID, PoemID)
                    , _con))
                    {
                        da.Fill(tbl);
                        if (1 == tbl.Rows.Count)
                            return
                                GetPoem(Convert.ToInt32(tbl.Rows[0].ItemArray[0]));
                    }
                }
            }
            return null;
        }
        public GanjoorPoem GetNextPoem(GanjoorPoem poem)
        {
            if (null != poem)
            {
                return GetNextPoem(poem._ID, poem._CatID);
            }
            return null;
        }
        public GanjoorPoem GetPreviousPoem(int PoemID, int CatID)
        {
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(
                        String.Format(
                        "SELECT ID FROM poem WHERE cat_id = {0} AND id<{1} ORDER BY ID DESC LIMIT 1"
                        ,
                        CatID, PoemID)
                    , _con))
                    {
                        da.Fill(tbl);
                        if (1 == tbl.Rows.Count)
                            return
                                GetPoem(Convert.ToInt32(tbl.Rows[0].ItemArray[0]));
                    }
                }
            }
            return null;
        }
        public GanjoorPoem GetPreviousPoem(GanjoorPoem poem)
        {
            if (null != poem)
            {
                return GetPreviousPoem(poem._ID, poem._CatID);
            }
            return null;
        }
        public GanjoorPoet GetPoet(int PoetID)
        {
            if (PoetID == 0)
                return new GanjoorPoet(0, "همه", 0);
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT id, name, cat_id FROM poet WHERE id = "+PoetID.ToString(), _con))
                    {
                        da.Fill(tbl);
                        System.Diagnostics.Debug.Assert(tbl.Rows.Count < 2);
                        if (1 == tbl.Rows.Count)
                            return
                                new GanjoorPoet(Convert.ToInt32(tbl.Rows[0].ItemArray[0]), tbl.Rows[0].ItemArray[1].ToString(), Convert.ToInt32(tbl.Rows[0].ItemArray[2]));
                        
                    }
                }
            }
            return null;
        }
        #endregion

        #region Search
        public DataTable FindPoemsContaingPhrase(string phrase, int PageStart, int Count, int PoetID)
        {
            if (Connected)
            {
                DataTable tbl = new DataTable();
                {
                    string strQuery = 
                        (PoetID == 0)
                        ?
                        "SELECT poem_id FROM verse WHERE text LIKE '%" + phrase + "%' GROUP BY poem_id LIMIT "+PageStart.ToString()+","+Count.ToString()
                        :
                        "SELECT poem_id FROM (verse INNER JOIN poem ON verse.poem_id=poem.id) INNER JOIN cat ON cat.id =cat_id WHERE verse.text LIKE '%" + phrase + "%' AND poet_id=" + PoetID.ToString() + " GROUP BY poem_id LIMIT " + PageStart.ToString() + "," + Count.ToString()
                        ;
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(strQuery, _con))
                    {
                        da.Fill(tbl);                        
                    }
                    return tbl;
                }
            }
            return null;
        }
        public DataTable FindFirstVerseContaingPhrase(int PoemID, string phrase)
        {
            if (Connected)
            {
                DataTable tbl = new DataTable();
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT text FROM verse WHERE poem_id="+PoemID+" AND text LIKE'%"+phrase+"%' LIMIT 0,1", _con))
                    {
                        da.Fill(tbl);
                    }
                    return tbl;
                }
            }
            return null;
        }
        #endregion

        #region Fav/UnFav
        private int MaxFavOrder
        {
            get
            {
                if (Connected)
                {
                    using (DataTable tbl = new DataTable())
                    {
                        using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT MAX(pos) FROM fav", _con))
                        {
                            da.Fill(tbl);
                            if (tbl.Rows.Count != 0)
                                if (tbl.Rows[0].ItemArray[0] is DBNull)
                                    return -1;
                                else
                                    return Convert.ToInt32(tbl.Rows[0].ItemArray[0]);
                            return -1;
                        }
                    }
                }
                return -1;
            }
        }
        public bool IsPoemFaved(int PoemID)
        {
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT pos FROM fav WHERE poem_id = " + PoemID.ToString(), _con))
                    {
                        da.Fill(tbl);
                        return tbl.Rows.Count != 0;
                    }
                }

            }
            return false;
        }
        public bool IsVerseFaved(int PoemID, int VerseID)
        {
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT pos FROM fav WHERE poem_id = " + PoemID.ToString() + " AND verse_id=" + VerseID.ToString(), _con))
                    {
                        da.Fill(tbl);
                        return tbl.Rows.Count != 0;
                    }
                }

            }
            return false;
        }
        public void Fav(int PoemID)
        {
            Fav(PoemID, -1);
        }
        public void Fav(int PoemID, int VerseID)
        {
            if (Connected)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(_con))
                {
                    cmd.CommandText = "INSERT INTO fav (poem_id, pos, verse_id) VALUES (" + PoemID.ToString() + ","+(MaxFavOrder+1).ToString()+","+VerseID.ToString()+");";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void UnFav(int PoemID)
        {
            UnFav(PoemID, -1);
        }
        public void UnFav(int PoemID, int VerseID)
        {
            if (Connected)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(_con))
                {
                    if(VerseID != -1)
                        cmd.CommandText = "DELETE FROM fav WHERE poem_id=" + PoemID.ToString()+" AND verse_id="+VerseID.ToString();
                    else
                        cmd.CommandText = "DELETE FROM fav WHERE poem_id=" + PoemID.ToString();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public bool ToggleFav(int PoemID, int VerseID)
        {
            bool faved = VerseID == -1 ? IsPoemFaved(PoemID) : IsVerseFaved(PoemID, VerseID);
            if (faved)
            {
                UnFav(PoemID, VerseID);
                return false;
            }
            else
            {
                Fav(PoemID, VerseID);
                return true;
            }
        }
        public DataTable GetFavs(int PageStart, int Count)
        {
            if (Connected)
            {
                DataTable tbl = new DataTable();
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT poem_id FROM fav GROUP BY poem_id ORDER BY pos LIMIT " + PageStart.ToString() + "," + Count.ToString(), _con))
                    {
                        da.Fill(tbl);
                    }
                    return tbl;
                }
            }
            return null;
        }
        public GanjoorVerse GetPreferablyAFavVerse(int PoemID)
        {
            List<GanjoorVerse> allVerses = GetVerses(PoemID);
            
            for (int v = 0; v < allVerses.Count; v++)
            {
                GanjoorVerse verse = allVerses[v];
                if (IsVerseFaved(verse._PoemID, verse._Order))
                {
                    return verse;
                }
            }
            if (allVerses.Count != 0)
                return allVerses[0];
            return null;
            
        }
        #endregion

        #region Random Poem
        public int GetRandomPoem(int CatID)
        {
            if (Connected)
            {
                using (DataTable tbl = new DataTable())
                {
                    string strQuery = "SELECT MIN(id), MAX(id) FROM poem";
                    if (CatID != 0)
                        strQuery += " WHERE cat_id=" + CatID;
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(strQuery, _con))
                    {
                        da.Fill(tbl);
                        if (tbl.Rows.Count > 0)
                        {
                            return rnd.Next(Convert.ToInt32(tbl.Rows[0].ItemArray[0]), Convert.ToInt32(tbl.Rows[0].ItemArray[1]));
                        }                        
                    }
                }
            }
            return 0;
        }
        private Random rnd = new Random();
        #endregion

        #region Versioning
        private const int DatabaseVersion = 1;
        private void UpgradeOldDbs()
        {
            using (DataTable tbl = _con.GetSchema("Tables"))
            {
                #region fav table
                DataRow[] favTable = tbl.Select("Table_Name='fav'");
                
                if (favTable.Length == 0)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(_con))
                    {
                        cmd.CommandText = "CREATE TABLE fav (poem_id INTEGER, verse_id INTEGER, pos INTEGER);";
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    try
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(_con))
                        {
                            cmd.CommandText = "SELECT verse_id FROM fav LIMIT 0,1";
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception exp)
                    {                        
                        if (exp.Message.IndexOf("verse_id") != -1)
                        {
                            
                            using (SQLiteCommand cmd = new SQLiteCommand(_con))
                            {
                                cmd.CommandText = "CREATE TABLE favcpy (poem_id INTEGER, verse_id INTEGER, pos INTEGER);";
                                cmd.ExecuteNonQuery();
                            }
                            using (DataTable fav = new DataTable())
                            {
                                using (SQLiteDataAdapter favAd = new SQLiteDataAdapter("SELECT poem_id, pos FROM fav", _con))
                                {
                                    favAd.Fill(fav);
                                    foreach (DataRow row in fav.Rows)
                                    {
                                        using (SQLiteCommand cmd = new SQLiteCommand(_con))
                                        {
                                            cmd.CommandText = "INSERT INTO favcpy (poem_id, verse_id, pos) VALUES (" + row.ItemArray[0].ToString() + ", -1, " + row.ItemArray[1].ToString() + ")";
                                            cmd.ExecuteNonQuery();
                                        }                         
                                    }
                                }
                            }
                            using (SQLiteCommand cmd = new SQLiteCommand(_con))
                            {
                                cmd.CommandText = "DROP TABLE fav";
                                cmd.ExecuteNonQuery();
                            }
                            using (SQLiteCommand cmd = new SQLiteCommand(_con))
                            {
                                cmd.CommandText = "CREATE TABLE fav (poem_id INTEGER, verse_id INTEGER, pos INTEGER);";
                                cmd.ExecuteNonQuery();
                            }
                             
                            using (DataTable fav = new DataTable())
                            {
                                using (SQLiteDataAdapter favAd = new SQLiteDataAdapter("SELECT poem_id, pos, verse_id FROM favcpy", _con))
                                {
                                    favAd.Fill(fav);
                                    foreach (DataRow row in fav.Rows)
                                    {
                                        using (SQLiteCommand cmd = new SQLiteCommand(_con))
                                        {
                                            cmd.CommandText = "INSERT INTO fav (poem_id, verse_id, pos) VALUES (" + row.ItemArray[0].ToString() + ", -1, " + row.ItemArray[1].ToString() + ")";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                            using (SQLiteCommand cmd = new SQLiteCommand(_con))
                            {
                                cmd.CommandText = "DROP TABLE favcpy";
                                cmd.ExecuteNonQuery();
                            }

                        }
                    }

                }
                #endregion
                #region version table
                DataRow[] verTable = tbl.Select("Table_Name='gver'");

                if (verTable.Length == 0 && File.Exists(Path.GetDirectoryName(Application.ExecutablePath)+"\\vg.s3db"))
                {
                    MessageBox.Show("پایگاه داده‌های برنامه قدیمی است. فرایند بروزرسانی آن ممکن است تا چند دقیقه طول بکشد. لطفاً صبور باشید.", "لطفاً صبور باشید", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    //Version table does not exist, so our verse table
                    //position field values are incorrect,
                    //correct it using vg.s3db file and then create version table

                    SQLiteConnectionStringBuilder conString = new SQLiteConnectionStringBuilder();
                    conString.DataSource = "vg.s3db";
                    conString.DefaultTimeout = 5000;
                    conString.FailIfMissing = true;
                    conString.ReadOnly = true;
                    using (SQLiteConnection vgCon = new SQLiteConnection(conString.ConnectionString))
                    {
                        vgCon.Open();
                        using (SQLiteCommand cmd = new SQLiteCommand(_con))
                        {
                            cmd.CommandText = "BEGIN TRANSACTION;";
                            cmd.ExecuteNonQuery();
                            using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT poem_id, vorder, position FROM verse", vgCon))
                            {
                                using (DataTable tblVerse = new DataTable())
                                {
                                    da.Fill(tblVerse);
                                    int poemID = 0;
                                    int vOrder = 1;
                                    int position = 2;
                                    foreach (DataRow row in tblVerse.Rows)
                                    {
                                        cmd.CommandText = "UPDATE verse SET position=" + row.ItemArray[position].ToString() + " WHERE poem_id = " + row.ItemArray[poemID].ToString() + " AND vorder=" + row.ItemArray[vOrder].ToString() + ";";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            cmd.CommandText = "CREATE TABLE gver (curver INTEGER);";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "INSERT INTO gver (curver) VALUES (" + DatabaseVersion.ToString() + ");";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "COMMIT;";
                            cmd.ExecuteNonQuery();
                        }
                    }
                    File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + "\\vg.s3db");
                }
                #endregion
                #region new poets
                if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + "\\new.s3db"))
                {
                    ImportDb(Path.GetDirectoryName(Application.ExecutablePath) + "\\new.s3db");
                    File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + "\\new.s3db");
                }
                #endregion
            }
        }
        #endregion

        #region Import Db
        public bool ImportDb(string fileName)
        {
            SQLiteConnection newConnection;
            try
            {
                SQLiteConnectionStringBuilder conString = new SQLiteConnectionStringBuilder();
                conString.DataSource = fileName;
                conString.DefaultTimeout = 5000;
                conString.FailIfMissing = true;
                conString.ReadOnly = false;
                newConnection = new SQLiteConnection(conString.ConnectionString);
                newConnection.Open();
            }
            catch (Exception exp)
            {
                LastError = exp.ToString();
                return false;
            }

            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand(_con))
                {
                    cmd.CommandText = "BEGIN TRANSACTION;";
                    cmd.ExecuteNonQuery();

                    using (DataTable tbl = new DataTable())
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT id, poet_id, text, parent_id, url FROM cat", newConnection))
                    {
                        da.Fill(tbl);
                        foreach (DataRow row in tbl.Rows)
                        {
                            cmd.CommandText = String.Format(
                                "INSERT INTO cat (id, poet_id, text, parent_id, url) VALUES ({0}, {1}, \"{2}\", {3}, \"{4}\");",
                                row.ItemArray[0], row.ItemArray[1], row.ItemArray[2], row.ItemArray[3], row.ItemArray[4]
                                );
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (DataTable tbl = new DataTable())
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT id, name, cat_id FROM poet", newConnection))
                    {
                        da.Fill(tbl);
                        foreach (DataRow row in tbl.Rows)
                        {
                            cmd.CommandText = String.Format(
                                "INSERT INTO poet (id, name, cat_id) VALUES ({0}, \"{1}\", {2});",
                                row.ItemArray[0], row.ItemArray[1], row.ItemArray[2]
                                );
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (DataTable tbl = new DataTable())
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT id, cat_id, title, url FROM poem", newConnection))
                    {
                        da.Fill(tbl);
                        foreach (DataRow row in tbl.Rows)
                        {
                            cmd.CommandText = String.Format(
                                "INSERT INTO poem (id, cat_id, title, url) VALUES ({0}, {1}, \"{2}\", \"{3}\");",
                                row.ItemArray[0], row.ItemArray[1], row.ItemArray[2], row.ItemArray[3]
                                );
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (DataTable tbl = new DataTable())
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT poem_id, vorder, position, text FROM verse", newConnection))
                    {
                        da.Fill(tbl);
                        foreach (DataRow row in tbl.Rows)
                        {
                            cmd.CommandText = String.Format(
                                "INSERT INTO verse (poem_id, vorder, position, text) VALUES ({0}, {1}, {2}, \"{3}\");",
                                row.ItemArray[0], row.ItemArray[1], row.ItemArray[2], row.ItemArray[3]
                                );
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.CommandText = "COMMIT;";
                    cmd.ExecuteNonQuery();

                }
            }
            catch (Exception exp)
            {
                LastError = exp.ToString();//probable repeated data
                newConnection.Dispose();
                return false;
            }

            newConnection.Dispose();
            
            return true;
        }
        #endregion

    }
}
