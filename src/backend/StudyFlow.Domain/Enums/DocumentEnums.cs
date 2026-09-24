namespace StudyFlow.Domain.Enums;

public enum DocumentFileType { Pdf, Docx, Pptx, Txt }
public enum DocumentProcessingStatus { Uploaded, Queued, Processing, Ready, Failed }
public enum DocumentProcessingJobStatus { Queued, Processing, Completed, Failed }
