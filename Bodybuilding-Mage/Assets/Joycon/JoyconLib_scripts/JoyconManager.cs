using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System;

public class JoyconManager : MonoBehaviour
{
    public bool EnableIMU = true;
    public bool EnableLocalize = true;

    private const ushort vendor_id = 0x57e;
    private const ushort vendor_id_ = 0x057e;
    private const ushort product_l = 0x2006;
    private const ushort product_r = 0x2007;

    public List<Joycon> j;
    static JoyconManager instance;

    public static JoyconManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        j = new List<Joycon>();

        HIDapi.hid_init();

        int count = EnumerateJoycons(vendor_id);
        if (count == 0)
            EnumerateJoycons(vendor_id_);
    }

    void Start()
    {
        for (int i = 0; i < j.Count; ++i)
        {
            byte LEDs = (byte)(0x1 << i);
            j[i].Attach(leds_: LEDs);
            j[i].Begin();
        }
    }

    void Update()
    {
        for (int i = 0; i < j.Count; ++i)
        {
            j[i].Update();
        }
    }

    private int EnumerateJoycons(ushort vendorId)
    {
        int count = 0;
        IntPtr ptr = HIDapi.hid_enumerate(vendorId, 0x0);
        IntPtr topPtr = ptr;

        while (ptr != IntPtr.Zero)
        {
            hid_device_info enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));

            if (enumerate.product_id == product_l || enumerate.product_id == product_r)
            {
                bool isLeft = enumerate.product_id == product_l;
                Debug.Log(isLeft ? "Left Joy-Con connected." : "Right Joy-Con connected.");

                IntPtr handle = HIDapi.hid_open_path(enumerate.path);
                HIDapi.hid_set_nonblocking(handle, 0);
                j.Add(new Joycon(handle, EnableIMU, EnableLocalize & EnableIMU, 0.05f, isLeft));
                count++;
            }

            ptr = enumerate.next;
        }

        if (topPtr != IntPtr.Zero)
            HIDapi.hid_free_enumeration(topPtr);

        return count;
    }

    void OnApplicationQuit()
    {
        for (int i = 0; i < j.Count; ++i)
        {
            j[i].Detach();
        }

        HIDapi.hid_exit();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
