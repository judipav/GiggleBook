using System;
using GiggleBook.Interfaces;

namespace GiggleBook.Services;

public class GlobalVariables : IGlobalVariables
{
    private int currentSlave;
    public int CurrentSlave 
    { 
        get => currentSlave;
        set => Interlocked.Exchange(ref currentSlave, value); 
    }
}
