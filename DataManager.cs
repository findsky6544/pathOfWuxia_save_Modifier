using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Heluo;
using Heluo.Data;
using Heluo.Resource;
using Heluo.Utility;

namespace 侠之道存档修改器
{

	public class DataManager : IDataProvider
	{

		public DataManager()
		{
			this.Reset();
		}

		public virtual void Reset()
		{
			this.ReadData("config\\chs\\textfiles");
		}

		protected virtual void ReadData(string path)
		{
			this.dict = new Dictionary<Type, IDictionary>();
			Type type = typeof(Item);
			foreach (Type itemType in from t in type.Assembly.GetTypes()
									  where t.IsSubclassOf(type) && !t.HasAttribute<Hidden>(false)
									  select t)
			{
				Type typeItemDic = typeof(CsvDataSource<>).MakeGenericType(new Type[]
				{
					itemType
				});
				try
				{
					byte[] fileData = null;
					IDictionary itemDic;
					string textAsset = File.ReadAllText(path + "\\" + itemType.Name + ".txt");
					fileData = System.Text.Encoding.UTF8.GetBytes(textAsset);
					itemDic = (Activator.CreateInstance(typeItemDic, new object[] { fileData }) as IDictionary);
					this.dict.Add(itemType, itemDic);
				}
				catch (Exception ex)
				{
					
				}
			}
		}

		public void Add<T>(T item) where T : Item
		{
			Type typeFromHandle = typeof(T);
			if (this.dict.ContainsKey(typeFromHandle))
			{
				if (this.dict[typeFromHandle] == null)
				{
					this.dict[typeFromHandle] = new Dictionary<string, T>();
				}
				this.dict[typeFromHandle].Add(item.Id, item);
				return;
			}
			this.dict.Add(typeFromHandle, new Dictionary<string, T>
			{
				{
					item.Id,
					item
				}
			});
		}

		public Dictionary<string, T> Get<T>() where T : Item
		{
			Type typeFromHandle = typeof(T);
			if (this.dict.ContainsKey(typeof(T)))
			{
				return this.dict[typeFromHandle] as Dictionary<string, T>;
			}
			return null;
		}

		public List<T> Get<T>(params string[] id) where T : Item
		{
			List<T> list = new List<T>();
			for (int i = 0; i < id.Length; i++)
			{
				T t = this.dict[typeof(T)][id] as T;
				if (t != null)
				{
					list.Add(t);
				}
			}
			return list;
		}

		public T Get<T>(string id) where T : Item
		{
			if (id.IsNullOrEmpty())
			{
				return default(T);
			}
			if (!this.dict.ContainsKey(typeof(T)))
			{
				return default(T);
			}
			if (!this.dict[typeof(T)].Contains(id))
			{
				return default(T);
			}
			return this.dict[typeof(T)][id] as T;
		}

		public List<T> Get<T>(Func<T, bool> filter) where T : Item
		{
			if (!this.dict.ContainsKey(typeof(T)))
			{
				return null;
			}
			List<T> list = new List<T>();
			foreach (object obj in this.dict[typeof(T)].Values)
			{
				if (filter(obj as T))
				{
					list.Add(obj as T);
				}
			}
			return list;
		}

		private void Save()
		{
		}

		public virtual Task RestAsync(IResourceProvider resource, string path = "Config/TextFiles/")
		{
			return null;
		}

		public T GetDefault<T>() where T : Item
		{
			return this.defaultValue[typeof(T)] as T;
		}

        public void Reset(IResourceProvider resource, string path)
        {
            throw new NotImplementedException();
        }

        public void Reset<T>(IResourceProvider resource, string path) where T : Item
        {
            throw new NotImplementedException();
        }

        protected IResourceProvider resource;

		protected IDictionary<Type, IDictionary> dict;

		private Dictionary<Type, Item> defaultValue = new Dictionary<Type, Item>();
	}
}
