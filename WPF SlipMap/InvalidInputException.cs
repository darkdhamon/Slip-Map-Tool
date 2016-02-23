using System;

namespace WPF_SlipMap
{
    public class InvalidInputException : Exception
    {
        public InvalidInputException(string message):base(message)
        {
            
        }
    }
}