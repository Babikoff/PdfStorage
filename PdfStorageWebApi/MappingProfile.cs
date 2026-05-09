using AutoMapper;
using Domain;
using DTO.Queue;
using DTO.WebApi;

namespace PdfStorageWebApi
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<NewDocumentDto, Document>();
            CreateMap<Document, NewDocumentResponseDto>();
        }
    }
}