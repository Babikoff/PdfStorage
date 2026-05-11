using AutoMapper;
using Domain;
using DTO.Queue;
using DTO.WebApi;

namespace MappingProfiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<QueueDocumentDto, Document>()
                .ForMember(d => d.FileText, o => o.Ignore())
                .ForMember(d => d.ProcessedAt, o => o.Ignore())
                ;
            CreateMap<Document, QueueDocumentDto>()
                .ForMember(d => d.RawFileData, o => o.Ignore());

            CreateMap<QueueDocumentDto, NewDocumentResponseDto>();
            CreateMap<Document, DocumentListItemDto>();
        }
    }
}