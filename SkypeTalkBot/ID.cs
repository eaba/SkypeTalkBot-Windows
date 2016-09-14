using System.Management;

class ID
{
    /// <summary>
    /// Oblicz odcisk palca dla tego komputera
    /// </summary>
    /// <returns>Unique ID</returns>
    public static string GetFingerprint()
    {
        var cpuId = GetCpuId();
        var biosId = GetBiosId();
        var motherboardId = GetMotherboardId();
        var toHash = cpuId + biosId + motherboardId;
        var hash = toHash.GetHashCode().ToString("X");
        return hash;
    }

    #region Źródło: http://www.codeproject.com/Articles/28678/Generating-Unique-Key-Finger-Print-for-a-Computer
    private static string GetCpuId()
    {
        //Uses first CPU identifier available in order of preference
        //Don't get all identifiers, as it is very time consuming
        string retVal = Identify("Win32_Processor", "UniqueId");
        if (retVal == "") //If no UniqueID, use ProcessorID
        {
            retVal = Identify("Win32_Processor", "ProcessorId");
            if (retVal == "") //If no ProcessorId, use Name
            {
                retVal = Identify("Win32_Processor", "Name");
                if (retVal == "") //If no Name, use Manufacturer
                {
                    retVal = Identify("Win32_Processor", "Manufacturer");
                }
                //Add clock speed for extra security
                retVal += Identify("Win32_Processor", "MaxClockSpeed");
            }
        }
        return retVal;
    }
    private static string GetBiosId()
    {
        return Identify("Win32_BIOS", "Manufacturer")
        + Identify("Win32_BIOS", "SMBIOSBIOSVersion")
        + Identify("Win32_BIOS", "IdentificationCode")
        + Identify("Win32_BIOS", "SerialNumber")
        + Identify("Win32_BIOS", "ReleaseDate")
        + Identify("Win32_BIOS", "Version");
    }
    private static string GetMotherboardId()
    {
        return Identify("Win32_BaseBoard", "Model")
        + Identify("Win32_BaseBoard", "Manufacturer")
        + Identify("Win32_BaseBoard", "Name")
        + Identify("Win32_BaseBoard", "SerialNumber");
    }
    private static string Identify(string wmiClass, string wmiProperty)
    {
        string result = string.Empty;
        ManagementClass mc = new ManagementClass(wmiClass);
        ManagementObjectCollection moc = mc.GetInstances();
        foreach (ManagementObject mo in moc)
        {
            //Only get the first one
            if (result == string.Empty)
            {
                try
                {
                    result = mo[wmiProperty].ToString();
                    break;
                }
                catch
                { }
            }
        }
        return result;
    }
    #endregion
}