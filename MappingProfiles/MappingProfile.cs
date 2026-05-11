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
            CreateMap<QueueDocumentDto, Document>();
            CreateMap<QueueDocumentDto, NewDocumentResponseDto>();
            CreateMap<Document, QueueDocumentDto>();
            CreateMap<Document, DocumentListItemDto>();
        }
    }
}