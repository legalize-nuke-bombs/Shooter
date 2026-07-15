package com.example.shooter.exception;

import lombok.Getter;

@Getter
public class ApiException extends RuntimeException {
    private final ErrorCode code;

    public ApiException(ErrorCode code) {
        this.code = code;
    }

    public ApiException(ErrorCode code, String detail) {
        super(detail);
        this.code = code;
    }
}
