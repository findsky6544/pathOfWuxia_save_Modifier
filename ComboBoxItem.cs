using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 侠之道存档修改器
{
    class ComboBoxItem
    {
        private string _key;
        private string _value;


         public ComboBoxItem(string key,string value)
        {
            this._key = key;
            this._value = value;
        }

        public string key
        {
            get { return _key; }
            set { _key = value; }
        }

        public string value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
