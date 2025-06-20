using System.Collections.Generic;
using System.Xml.Serialization;

namespace BalanceAct.Models;

/*   💰💰 [Data Model for OFX] 💰💰
 *   
 *   OFX (Open Financial Exchange) files are a file format used for exchanging financial data.
 *   Used to xfer financial information between different applications and online financial institutions.
 *   
 *   System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(OFX));
 *   using (StringReader reader = new StringReader(xml))
 *   {
 *       var tranList = (OFX)serializer.Deserialize(reader);
 *       foreach (var t in tranList.CREDITCARDMSGSRSV1.CCSTMTTRNRS.CCSTMTRS.BANKTRANLIST.STMTTRN)
 *       {
 *           Debug.WriteLine($"Transaction: {t.TRNTYPE} ⇨ {t.DTPOSTED} ⇨ {t.TRNAMT} ⇨ {t.NAME} ⇨ {t.FITID}");
 *       }
 *   }
 */
[XmlRoot(ElementName = "OFX")]
public class OFX
{

    [XmlElement(ElementName = "SIGNONMSGSRSV1")]
    public SIGNONMSGSRSV1 SIGNONMSGSRSV1 { get; set; }

    [XmlElement(ElementName = "CREDITCARDMSGSRSV1")]
    public CREDITCARDMSGSRSV1 CREDITCARDMSGSRSV1 { get; set; }
}

[XmlRoot(ElementName = "STATUS")]
public class STATUS
{

    [XmlElement(ElementName = "CODE")]
    public int CODE { get; set; }

    [XmlElement(ElementName = "SEVERITY")]
    public string SEVERITY { get; set; }

    [XmlElement(ElementName = "MESSAGE")]
    public string MESSAGE { get; set; }
}

[XmlRoot(ElementName = "FI")]
public class FI
{

    [XmlElement(ElementName = "ORG")]
    public string ORG { get; set; }

    [XmlElement(ElementName = "FID")]
    public int FID { get; set; }
}

[XmlRoot(ElementName = "SONRS")]
public class SONRS
{

    [XmlElement(ElementName = "STATUS")]
    public STATUS STATUS { get; set; }

    [XmlElement(ElementName = "DTSERVER")]
    public string DTSERVER { get; set; }

    [XmlElement(ElementName = "LANGUAGE")]
    public string LANGUAGE { get; set; }

    [XmlElement(ElementName = "FI")]
    public FI FI { get; set; }
}

[XmlRoot(ElementName = "SIGNONMSGSRSV1")]
public class SIGNONMSGSRSV1
{

    [XmlElement(ElementName = "SONRS")]
    public SONRS SONRS { get; set; }
}

[XmlRoot(ElementName = "CCACCTFROM")]
public class CCACCTFROM
{

    [XmlElement(ElementName = "ACCTID")]
    public string ACCTID { get; set; }
}

[XmlRoot(ElementName = "STMTTRN")]
public class STMTTRN
{

    [XmlElement(ElementName = "TRNTYPE")]
    public string TRNTYPE { get; set; }

    [XmlElement(ElementName = "DTPOSTED")]
    public string DTPOSTED { get; set; }

    [XmlElement(ElementName = "TRNAMT")]
    public double TRNAMT { get; set; }

    [XmlElement(ElementName = "FITID")]
    public string FITID { get; set; } // changed from double to string because of scientific notation format issue

    [XmlElement(ElementName = "NAME")]
    public string NAME { get; set; }
}

[XmlRoot(ElementName = "BANKTRANLIST")]
public class BANKTRANLIST
{

    [XmlElement(ElementName = "DTSTART")]
    public string DTSTART { get; set; }

    [XmlElement(ElementName = "DTEND")]
    public string DTEND { get; set; }

    [XmlElement(ElementName = "STMTTRN")]
    public List<STMTTRN> STMTTRN { get; set; }
}

[XmlRoot(ElementName = "LEDGERBAL")]
public class LEDGERBAL
{

    [XmlElement(ElementName = "BALAMT")]
    public double BALAMT { get; set; }

    [XmlElement(ElementName = "DTASOF")]
    public string DTASOF { get; set; }
}

[XmlRoot(ElementName = "AVAILBAL")]
public class AVAILBAL
{

    [XmlElement(ElementName = "BALAMT")]
    public double BALAMT { get; set; }

    [XmlElement(ElementName = "DTASOF")]
    public string DTASOF { get; set; }
}

[XmlRoot(ElementName = "CCSTMTRS")]
public class CCSTMTRS
{

    [XmlElement(ElementName = "CURDEF")]
    public string CURDEF { get; set; }

    [XmlElement(ElementName = "CCACCTFROM")]
    public CCACCTFROM CCACCTFROM { get; set; }

    [XmlElement(ElementName = "BANKTRANLIST")]
    public BANKTRANLIST BANKTRANLIST { get; set; }

    [XmlElement(ElementName = "LEDGERBAL")]
    public LEDGERBAL LEDGERBAL { get; set; }

    [XmlElement(ElementName = "AVAILBAL")]
    public AVAILBAL AVAILBAL { get; set; }
}

[XmlRoot(ElementName = "CCSTMTTRNRS")]
public class CCSTMTTRNRS
{

    [XmlElement(ElementName = "TRNUID")]
    public int TRNUID { get; set; }

    [XmlElement(ElementName = "STATUS")]
    public STATUS STATUS { get; set; }

    [XmlElement(ElementName = "CCSTMTRS")]
    public CCSTMTRS CCSTMTRS { get; set; }
}

[XmlRoot(ElementName = "CREDITCARDMSGSRSV1")]
public class CREDITCARDMSGSRSV1
{

    [XmlElement(ElementName = "CCSTMTTRNRS")]
    public CCSTMTTRNRS CCSTMTTRNRS { get; set; }
}
