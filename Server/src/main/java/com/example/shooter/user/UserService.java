package com.example.shooter.user;

import com.example.shooter.exception.ApiException;
import com.example.shooter.exception.ErrorCode;
import lombok.extern.slf4j.Slf4j;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.Instant;
import java.util.Arrays;
import java.util.List;
import java.util.Map;

@Service
@Slf4j
public class UserService {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final PasswordValidationService passwordValidationService;

    public UserService(UserRepository userRepository, PasswordEncoder passwordEncoder, PasswordValidationService passwordValidationService) {
        this.userRepository = userRepository;
        this.passwordEncoder = passwordEncoder;
        this.passwordValidationService = passwordValidationService;
    }

    public UserRepresentation getById(Long userId, Long targetId) {
        User user = userRepository.findById(targetId).orElseThrow(() -> new ApiException(ErrorCode.USER_NOT_FOUND));
        return new UserRepresentation(user);
    }

    public UserRepresentation getByUsername(Long userId, String targetUsername) {
        User user = userRepository.findByUsername(targetUsername).orElseThrow(() -> new ApiException(ErrorCode.USER_NOT_FOUND));
        return new UserRepresentation(user);
    }

    @Transactional
    public UserRepresentation patchMe(Long userId, PatchMeRequest request) {
        String displayName = request.getDisplayName();

        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));

        if (displayName == null) {
            throw new ApiException(ErrorCode.EMPTY_REQUEST);
        }

        if (displayName != null) user.setDisplayName(displayName);

        user = userRepository.save(user);

        log.info("user {} patched themselves 🔪", userId);
        return new UserRepresentation(user);
    }

    @Transactional
    public UserRepresentation patchMeSecure(Long userId, PatchMeSecureRequest request) {
        String currentPassword = request.getCurrentPassword();
        String username = request.getUsername();
        String password = request.getPassword();

        if (username == null && password == null) {
            throw new ApiException(ErrorCode.EMPTY_REQUEST);
        }

        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));

        if (!passwordEncoder.matches(currentPassword, user.getPasswordHash())) {
            log.info("secure patch rejected: invalid password for user {} ❌", user.getId());
            throw new ApiException(ErrorCode.INVALID_PASSWORD);
        }

        if (password != null) {
            passwordValidationService.validate(password);
            user.setPasswordHash(passwordEncoder.encode(password));
        }

        try {
            if (username != null) user.setUsername(username);
            userRepository.save(user);
        }
        catch (DataIntegrityViolationException e) {
            throw new ApiException(ErrorCode.USERNAME_TAKEN);
        }

        log.info("secure patch successful: user {} 🔪", user.getId());
        return new UserRepresentation(user);
    }

    @Transactional
    public void deleteMe(Long userId, PasswordRequest request) {
        String password = request.getPassword();

        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));

        if (!passwordEncoder.matches(password, user.getPasswordHash())) {
            log.info("delete rejected: invalid password for user {} ❌", user.getId());
            throw new ApiException(ErrorCode.INVALID_PASSWORD);
        }

        userRepository.delete(user);
        log.info("delete done successful: goodbye, user {} 🫡", userId);
    }
}
