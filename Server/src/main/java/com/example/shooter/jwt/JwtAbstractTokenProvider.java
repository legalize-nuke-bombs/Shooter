package com.example.shooter.jwt;

import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import lombok.extern.slf4j.Slf4j;
import javax.crypto.SecretKey;
import java.util.Base64;
import java.util.Date;
import java.util.Optional;

@Slf4j
public class JwtAbstractTokenProvider {

    private final SecretKey key;
    private final long expirationMs;

    public JwtAbstractTokenProvider(String secret, long expirationMs) {
        log.info("initialization: jwt secret length {} expiration ms {}", secret.length(), expirationMs);
        this.key = Keys.hmacShaKeyFor(Base64.getDecoder().decode(secret));
        this.expirationMs = expirationMs;
    }

    public String generateToken(String payload) {
        Date now = new Date();
        Date expiry = new Date(now.getTime() + expirationMs);

        return Jwts.builder()
                .subject(payload)
                .issuedAt(now)
                .expiration(expiry)
                .signWith(key)
                .compact();
    }

    public Optional<String> getPayloadFromToken(String token) {
        try {
            return Optional.of(
                    Jwts.parser()
                            .verifyWith(key)
                            .build()
                            .parseSignedClaims(token)
                            .getPayload()
                            .getSubject()
            );
        }
        catch (Exception e) {
            log.info("access rejected: {}", e.getMessage());
            return Optional.empty();
        }
    }
}
