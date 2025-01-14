﻿//       				<triggered_effect trigger="onSelfBuffStart" action="Teleport, SCore" location="Portal01" />
//       				<triggered_effect trigger="onSelfBuffStart" action="Teleport, SCore" location="destination=Portal01" />

using System.Collections.Generic;
using System.Xml;

public class MinEventActionTeleport : MinEventActionTargetedBase
{

    private string location;
    public override void Execute(MinEventParams _params)
    {
        var player = _params.Self as EntityPlayer;
        if (player == null) return;

        if ( string.IsNullOrEmpty(location) ) return;
        var destination = PortalManager.Instance.GetDestination(location);
        if ( destination == Vector3i.zero) return; // No portal

        player.Teleport(destination);
    }

    public override bool ParseXmlAttribute(XmlAttribute _attribute)
    {
        var flag = base.ParseXmlAttribute(_attribute);
        if (!flag)
        {
            var name = _attribute.Name;
            if (name != null)
            {
                if (name == "location")
                {
                    location = _attribute.Value;
                    return true;
                }
            }
        }

        return flag;
    }
}