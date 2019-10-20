using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
// ReSharper disable UnusedMember.Global

namespace DotNext.StaticAnalysis
{
	[XmlType("suppressions")]
	public class Suppressions
	{
		[XmlElement("suppressDiagnostic")]
		public Suppression[] Items { get; set; }
	}

	public struct Suppression : IXmlSerializable, IEquatable<Suppression>
	{
		public Suppression(string id, string context, string target)
		{
			Id = id;
			Context = context;
			Target = target;
		}

		public string Id { get; private set; }
		public string Context { get; private set; }
		public string Target { get; private set; }

		public XmlSchema GetSchema() => null;

		public void ReadXml(XmlReader reader)
		{
			reader.MoveToContent();
			Id = reader.GetAttribute("id");
			reader.ReadStartElement();
			Context = reader.ReadElementContentAsString("context", String.Empty);
			Target = reader.ReadElementContentAsString("target", String.Empty);
			reader.ReadEndElement();
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("id", Id);
			writer.WriteElementString("context", Context);
			writer.WriteElementString("target", Target);
		}

		public bool Equals(Suppression other)
		{
			return string.Equals(Id, other.Id) && string.Equals(Context, other.Context) && string.Equals(Target, other.Target);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Suppression suppression && Equals(suppression);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				// ReSharper disable NonReadonlyMemberInGetHashCode
				var hashCode = (Id != null ? Id.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Context != null ? Context.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
				// ReSharper restore NonReadonlyMemberInGetHashCode
				return hashCode;
			}
		}

		public static bool operator ==(Suppression left, Suppression right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Suppression left, Suppression right)
		{
			return !left.Equals(right);
		}
	}
}