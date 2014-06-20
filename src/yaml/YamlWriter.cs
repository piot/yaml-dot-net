using System;
using System.IO;

namespace yaml {
	internal class YamlWriter {
		public YamlWriter() {
		}

		bool IsPrimitive (Type t) {
			return t.IsPrimitive || t == typeof(Decimal) || t == typeof(String);
		}

		bool ShouldRecurse (Type t) {
			return !IsPrimitive(t);
		}

		bool ShouldRecurse (Object o) {
			return o != null && ShouldRecurse(o.GetType());
		}

		void WriteObject (Object o, StringWriter writer, int indent) {
			var t = o.GetType();

			var tabs = new String('\t', indent);

			var properties = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			foreach (var p in properties) {
				var subValue = p.GetValue(o, null);
				if (ShouldRecurse(subValue)) {
					writer.WriteLine("{0}{1}:", tabs, p.Name);
					WriteObject(subValue, writer, indent + 1);
				} else {
					if (subValue != null && subValue.GetType() == typeof(String)) {
						subValue = "'" + subValue + "'";
					}
					writer.WriteLine("{0}{1}: {2}", tabs, p.Name, subValue);
				}
			}
			var fields = t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			foreach (var f in fields) {
				var subValue = f.GetValue(o);
				if (ShouldRecurse(subValue)) {
					writer.WriteLine("{0}{1}:", tabs, f.Name);
					WriteObject(subValue, writer, indent + 1);
				} else {
					if (subValue != null && subValue.GetType() == typeof(String)) {
						subValue = "'" + subValue + "'";
					}
					writer.WriteLine("{0}{1}: {2}", tabs, f.Name, subValue);
				}
			}
		}

		public string Write (Object o) {
			var writer = new StringWriter();
			WriteObject(o, writer, 0);
			writer.Close();
			return writer.ToString();
		}
	}
}

