﻿namespace Datadock.Common.Stores
{
    public class DatasetNotFoundException : DatasetStoreException
    {
        public DatasetNotFoundException(string msg) : base(msg) { }
    }
}