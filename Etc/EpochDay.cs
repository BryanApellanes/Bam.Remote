using System;
using Bam.Net;

namespace Bam.Remote.Etc
{
    public class EpochDay
    {
        public static implicit operator DateTime(EpochDay epochDay)
        {
            return epochDay.ToDateTime();
        }

        public static implicit operator EpochDay(int value)
        {
            return new EpochDay(value);
        }
        
        public static DateTime Root => Instant.FromDateString("01/01/1970");

        public EpochDay()
        {
        }

        public EpochDay(int value)
        {
            Value = value;
        }

        public static EpochDay Today => DateTime.UtcNow.Subtract(Root).Days;

        public override string ToString()
        {
            return Value == 0 ? "" : Value.ToString();
        }

        public int Value { get; set; }

        public DateTime ToDateTime()
        {
            return Root.AddDays(Value);
        }

        public static DateTime FromInt(int value)
        {
            return new EpochDay(value).ToDateTime();
        } 
    }
}