using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentStorageWebApi.Controllers.Helpers
{
    public class QueryParameters
    {
        const int _minPageNum = 1;
        const int _maxSize = 100;
        private int _page = _minPageNum;
        private int _size = 50;

        public int Page 
        {
            get 
            {
                return _page;
            }
            set
            {
                _page = Math.Max(_minPageNum, value);
            }
        }

        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = Math.Min(_maxSize, value);
            }
        }

        public string SortBy { get; set; } = "Id";

        private string _sortOrder = "asc";
        public string SortOrder
        {
            get
            {
                return _sortOrder;
            }
            set
            {
                if (value == "asc" || value == "desc")
                {
                    _sortOrder = value;
                }
            }
        }
    }
}
