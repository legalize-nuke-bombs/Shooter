package com.example.shooter.exception;

import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ProblemDetail;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.multipart.MultipartException;
import org.springframework.web.servlet.mvc.method.annotation.ResponseEntityExceptionHandler;

@RestControllerAdvice
@Slf4j
public class MainExceptionHandler extends ResponseEntityExceptionHandler {

    @ExceptionHandler(ApiException.class)
    public ProblemDetail handleApi(ApiException e) {
        ProblemDetail problem = ProblemDetail.forStatus(e.getCode().status);
        problem.setProperty("code", e.getCode().name());
        if (e.getMessage() != null) {
            problem.setDetail(e.getMessage());
        }
        return problem;
    }

    @ExceptionHandler(MultipartException.class)
    public ProblemDetail handleMultipart(MultipartException e) {
        log.warn("multipart upload exception: {}", e.getMessage());
        ProblemDetail problem = ProblemDetail.forStatus(ErrorCode.MALFORMED_REQUEST.status);
        problem.setProperty("code", ErrorCode.MALFORMED_REQUEST.name());
        return problem;
    }

    @ExceptionHandler(Exception.class)
    public ProblemDetail handleUnexpected(Exception e) {
        log.error("unhandled exception", e);
        ProblemDetail problem = ProblemDetail.forStatus(ErrorCode.INTERNAL_ERROR.status);
        problem.setProperty("code", ErrorCode.INTERNAL_ERROR.name());
        return problem;
    }
}
