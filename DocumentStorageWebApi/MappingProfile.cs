using AutoMapper;
using Domain;
using DTO.Queue;
using DTO.WebApi;

namespace DocumentStorageWebApi
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<NewDocumentDto, Document>();
            CreateMap<Document, NewDocumentResponseDto>();
            CreateMap<Document, DocumentListItemDto>();
        }
    }
}