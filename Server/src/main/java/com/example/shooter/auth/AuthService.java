package com.example.shooter.auth;

import com.example.shooter.jwt.JwtTokenProvider;
import com.example.shooter.user.*;
import lombok.extern.slf4j.Slf4j;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import com.example.shooter.exception.ApiException;
import com.example.shooter.exception.ErrorCode;

import java.time.Instant;

@Service
@Slf4j
public class AuthService {

    private final UserRepository userRepository;
    private final JwtTokenProvider jwtTokenProvider;
    private final PasswordEncoder passwordEncoder;
    private final PasswordValidationService passwordValidationService;

    public AuthService(UserRepository userRepository, JwtTokenProvider jwtTokenProvider, PasswordEncoder passwordEncoder, PasswordValidationService passwordValidationService) {
        this.userRepository = userRepository;
        this.jwtTokenProvider = jwtTokenProvider;
        this.passwordEncoder = passwordEncoder;
        this.passwordValidationService = passwordValidationService;
    }

    @Transactional
    public RegisterResponse register(RegisterRequest request) {
        String username = request.getUsername();
        String displayName = request.getDisplayName();
        String password = request.getPassword();

        passwordValidationService.validate(password);

        User user;
        try {
            user = new User();
            user.setUsername(username);
            user.setDisplayName(displayName);
            user.setRegisteredAt(Instant.now().getEpochSecond());
            user.setPasswordHash(passwordEncoder.encode(password));
            user = userRepository.save(user);
        }
        catch (DataIntegrityViolationException e) {
            throw new ApiException(ErrorCode.USERNAME_TAKEN);
        }

        log.info("registration successful: user {}", user.getId());
        return new RegisterResponse(
                jwtTokenProvider.generateToken(String.valueOf(user.getId()))
        );
    }

    public LoginResponse login(LoginRequest request) {
        String username = request.getUsername();
        String password = request.getPassword();

        User user = userRepository.findByUsername(username).orElseThrow(() -> new ApiException(ErrorCode.USER_NOT_FOUND));
        if (!passwordEncoder.matches(password, user.getPasswordHash())) {
            log.info("login rejected: invalid password for user {}", user.getId());
            throw new ApiException(ErrorCode.INVALID_PASSWORD);
        }

        log.info("login successful: user {}", user.getId());
        return new LoginResponse(jwtTokenProvider.generateToken(String.valueOf(user.getId())));
    }
}
