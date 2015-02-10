using System;
using System.Collections.Generic;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

private static void StartScan()
{
	foreach(NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
		if (ni.OperationalStatus == OperationalStatus.Down)
			continue;
		if (ni.GetIPProperties().GatewayAddresses.Count == 0)
			continue;
		foreach(UnicastIPAddressInformation uipi in ni.GetIPProperties().UnicastAddresses) {
			if (uipi.IPv4Mask == null)
				continue;
			System.Console.WriteLine("IP: " + uipi.Address + ", Netmask: " + uipi.IPv4Mask);
			String[]IPParts = uipi.Address.ToString().Split('.');
			String[]NetmaskParts = uipi.IPv4Mask.ToString().Split('.');
			String StartIP;
			StartIP = (int.Parse(IPParts[0]) & (int.Parse(NetmaskParts[0]))) +"." + (int.Parse(IPParts[1]) & (int.Parse(NetmaskParts[1]))) +"." + (int.Parse(IPParts[2]) & (int.Parse(NetmaskParts[2]))) +"." + (int.Parse(IPParts[3]) & (int.Parse(NetmaskParts[3])));
			String EndIP;
			String[]StartIPParts = StartIP.Split('.');
			EndIP = (int.Parse(StartIPParts[0]) + 255 - (int.Parse(NetmaskParts[0]))) +"." + (int.Parse(StartIPParts[1]) + 255 - (int.Parse(NetmaskParts[1]))) +"." + (int.Parse(StartIPParts[2]) + 255 - (int.Parse(NetmaskParts[2]))) +"." + (int.Parse(StartIPParts[3]) + 255 - (int.Parse(NetmaskParts[3])));
			System.Console.WriteLine("StartIP: " + StartIP);
			System.Console.WriteLine("EndIP : " + EndIP);
			String ItemIP, ItemMAC, ItemName;
			for (int o0 = int.Parse(StartIP.Split('.')[0]); o0 <= int.Parse(EndIP.Split('.')[0]); o0++)
				for (int o1 = int.Parse(StartIP.Split('.')[1]); o1 <= int.Parse(EndIP.Split('.')[1]); o1++)
					for (int o2 = int.Parse(StartIP.Split('.')[2]); o2 <= int.Parse(EndIP.Split('.')[2]); o2++)
						for (int o3 = int.Parse(StartIP.Split('.')[3]); o3 <= int.Parse(EndIP.Split('.')[3]); o3++) {
							if ((o3 == 0) || (o3 == 255))
								continue;
							String MAC = GetMacFromIP(IPAddress.Parse(o0 + "." + o1 + "." + o2 + "." + o3));
							if (MAC == "00:00:00:00:00:00")
								continue;
							ItemIP = o0 + "." + o1 + "." + o2 + "." + o3;
							ItemMAC = GetMacFromIP(IPAddress.Parse(o0 + "." + o1 + "." + o2 + "." + o3));
							String[]Item = new String[2];
							Item[0] = ItemMAC;
							Item[1] = ItemIP;																																/** You can add Item[] to any collection */
							Console.WriteLine(Item[0] + " --> " + Item[1]);
						}
		}
	}
	System.Console.WriteLine("Scan Ended");
}

[System.Runtime.InteropServices.DllImport("Iphlpapi.dll", EntryPoint = "SendARP")]
internal extern static Int32 SendArp(Int32 destIpAddress, Int32 srcIpAddress, byte[] macAddress, ref Int32 macAddressLength);

public static String GetMacFromIP(System.Net.IPAddress IP)
{
	if (IP.AddressFamily != AddressFamily.InterNetwork)
		throw new ArgumentException("suppoerts just IPv4 addresses");

	Int32 addrInt = IpToInt(IP);
	Int32 srcAddrInt = IpToInt(IP);

	byte[] mac = new byte[6]; // 48 bit
	int length = mac.Length;
	int reply = SendArp(addrInt, srcAddrInt, mac, ref length);

	String rawMac = new System.Net.NetworkInformation.PhysicalAddress(mac).ToString();
	String newMac = Regex.Replace(rawMac, "(..)(..)(..)(..)(..)(..)", "$1:$2:$3:$4:$5:$6");

	return newMac;
}

private static Int32 IpToInt(System.Net.IPAddress IP)
{
	byte[] bytes = IP.GetAddressBytes();
	return BitConverter.ToInt32(bytes, 0);
}