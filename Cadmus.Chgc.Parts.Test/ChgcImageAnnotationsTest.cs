using System;
using Xunit;
using System.Collections.Generic;
using Cadmus.Img.Parts;
using Cadmus.Core;
using System.Linq;

namespace Cadmus.Chgc.Parts.Test;

public sealed class ChgcImageAnnotationsPartTest
{
    private static ChgcImageAnnotationsPart GetPart()
    {
        ChgcImageAnnotationsPart part = new()
        {
            ItemId = Guid.NewGuid().ToString(),
            Id = Guid.NewGuid().ToString(),
            RoleId = "some-role",
            CreatorId = "zeus",
            UserId = "another",
        };
        part.Annotations.Add(new ChgcImageAnnotation()
        {
            Target = new GalleryImage { Uri = "https://some-image.jpg" },
            Selector = "some-selector",
            Notes = new List<string> { "note 1", "note 2" },
            Tags = new List<string> { "tag" },
            Eid = "n_alpha",
        });
        return part;
    }

    [Fact]
    public void Part_Is_Serializable()
    {
        ChgcImageAnnotationsPart part = GetPart();

        string json = TestHelper.SerializePart(part);
        ChgcImageAnnotationsPart part2 =
            TestHelper.DeserializePart<ChgcImageAnnotationsPart>(json)!;

        Assert.Equal(part.Id, part2.Id);
        Assert.Equal(part.TypeId, part2.TypeId);
        Assert.Equal(part.ItemId, part2.ItemId);
        Assert.Equal(part.RoleId, part2.RoleId);
        Assert.Equal(part.CreatorId, part2.CreatorId);
        Assert.Equal(part.UserId, part2.UserId);
    }

    [Fact]
    public void GetDataPins_Entries_Ok()
    {
        ChgcImageAnnotationsPart part = new()
        {
            ItemId = Guid.NewGuid().ToString(),
            RoleId = "some-role",
            CreatorId = "zeus",
            UserId = "another",
        };

        for (int n = 1; n <= 3; n++)
        {
            part.Annotations.Add(new ChgcImageAnnotation
            {
                Id = $"#a{n}",
                Eid = $"#a{n}",
                Tags = new List<string> { $"eid_img-anno-{n}" },
                Target = new GalleryImage
                {
                    Id = $"#i{n}"
                }
            });
        }

        List<DataPin> pins = part.GetDataPins(null).ToList();

        Assert.Equal(7, pins.Count);

        DataPin? pin = pins.Find(p => p.Name == "tot-count");
        Assert.NotNull(pin);
        TestHelper.AssertPinIds(part, pin!);
        Assert.Equal("3", pin!.Value);

        for (int n = 1; n <= 3; n++)
        {
            // id
            pin = pins.Find(p => p.Name == "id" && p.Value == $"#a{n}");
            Assert.NotNull(pin);
            TestHelper.AssertPinIds(part, pin!);

            // eid
            pin = pins.Find(p => p.Name == "eid" && p.Value == $"#a{n}");
            Assert.NotNull(pin);
            TestHelper.AssertPinIds(part, pin!);
        }
    }
}