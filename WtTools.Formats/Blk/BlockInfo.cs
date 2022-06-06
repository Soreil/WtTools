using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WtTools.Formats.Blk;

internal struct BlockInfo
{
    public int Id;
    public string Name;
    public int BlockOffset;
    public ParamInfo[] Params;
    public BlockInfo[]? Blocks;

    public BlockInfo(BinaryReader reader, BlkInfo blk)
    {
        Id = reader.Read7BitEncodedInt() - 1;
        var paramsCount = reader.Read7BitEncodedInt();
        var blocksCount = reader.Read7BitEncodedInt();
        Params = new ParamInfo[paramsCount];
        Blocks = new BlockInfo[blocksCount];
        BlockOffset = blocksCount > 0 ? reader.Read7BitEncodedInt() : 0;
        Name = Id >= 0 ? blk.GetStringValue(Id) : string.Empty;
    }

    public BlockInfo(ParamInfo[] @params)
    {
        Params = @params;
        Id = -1;
        Name = string.Empty;
        Blocks = null;
        BlockOffset = 0;
    }

    //We have to type this since we are working with parambase objects now instead of just generic object
    //The JSON deserializer does not know how to access the underlying values in the parambase objects
    //correctly
    public Dictionary<string, object> ToDictionary()
    {
        var result = new Dictionary<string, object>();
        if (Params is not null)
        {
            foreach (var item in Params)
            {
                //If we already have a copy of a parameter with this item name
                if (result.ContainsKey(item.Name))
                {
                    //If this item is already stored as a list we can just append to it
                    if (result[item.Name] is List<object> list)
                    {
                        list.Add(item.Value);
                    }
                    //If this item is stored as just a single value we will have to turn it in to a list and append the next values
                    else
                    {
                        var temp = new List<object>
                        {
                            result[item.Name],
                            item.Value
                        };
                        result[item.Name] = temp;
                    }
                }
                //New item found, add it as a single item
                else
                {
                    result.Add(item.Name, item.Value);
                }
            }
        }
        if (Blocks is not null)
        {
            foreach (var item in Blocks)
            {
                if (result.ContainsKey(item.Name))
                {
                    if (result[item.Name] is List<object> list)
                    {
                        list.Add(item.ToDictionary());
                    }
                    else
                    {
                        var temp = new List<object>
                        {
                            result[item.Name],
                            item.ToDictionary()
                        };
                        result[item.Name] = temp;
                    }
                }
                else
                {
                    result.Add(item.Name, item.ToDictionary());
                }
            }
        }
        return result;
    }

    public string ToStrict()
    {
        var builder = new StringBuilder();
        foreach (var item in Params)
        {
            _ = builder.AppendLine($"{item.Name}:{((DataName)item.Type).ToString().ToLower()}={item.Value}");
        }
        if (Blocks is not null)
        {
            var blockStrings = Blocks.ToList()
                                     .ConvertAll(x => string.Join("\n  ", x.ToStrict().Split('\n')));
            foreach (var b in blockStrings) builder.AppendLine(b);
        }
        return builder.ToString();
    }
}
