﻿namespace ToSic.Sxc.Data
{
    public class DataPublishing
    {
        public bool Enabled { get; set; }
        public string Streams { get; set; }

        public DataPublishing()
        {
            Enabled = false;
            Streams = "";
        }

    }

    

}