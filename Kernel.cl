__kernel void imagingTest(__read_only  image2d_t srcImg,
                       __write_only image2d_t dstImg)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP_TO_EDGE | //Clamp to zeros
    CLK_FILTER_LINEAR;
  int2 coord = (int2)(get_global_id(0), get_global_id(1));
  uint4 bgra = read_imageui(srcImg, smp, coord); //The byte order is BGRA
  float4 bgrafloat = convert_float4(bgra) / 255.0f; //Convert to normalized [0..1] float
  //Convert RGB to luminance (make the image grayscale).
  float luminance =  sqrt(0.241f * bgrafloat.z * bgrafloat.z + 0.691f * 
                      bgrafloat.y * bgrafloat.y + 0.068f * bgrafloat.x * bgrafloat.x);
  bgra.x = bgra.y = bgra.z = (uint) (luminance * 255.0f);
  bgra.w = 255;
  write_imageui(dstImg, coord, bgra);
}

__kernel void sobelFilter(__read_only  image2d_t srcImg,
                          __write_only image2d_t dstImg)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP_TO_EDGE | //Clamp to zeros
    CLK_FILTER_LINEAR;
  
  int2 relative_coords[9] = {
    (int2)(-1, -1), (int2)(0, -1), (int2)(1, -1),
    (int2)(-1, 0), (int2)(0, 0), (int2)(1, 0),
    (int2)(-1, 1), (int2)(0, 1), (int2) (1, 1)
  };

  // right direction convolution kernel
  float right_kernel[9] = {
    1, 0, -1,
    2, 0, -2,
    1, 0, -1
  };

  // down direction convolution kernel
  float down_kernel[9] = {
    1, 2, 1,
    0, 0, 0,
    -1, -2, -1
  };

  int2 base_coord = (int2)(get_global_id(0), get_global_id(1));

  float right_direction = 0.0f;
  float down_direction = 0.0f;
  for(int i = 0; i < 9; i++) {
    float right_element = right_kernel[i];
    float down_element = down_kernel[i];

    // get target pixel's bgra value, then convert it to grayscale
    int2 coord = base_coord + relative_coords[i];
    uint4 bgra = read_imageui(srcImg, smp, coord);
    float4 bgrafloat = convert_float4(bgra) / 255.0f;
    float luminance = sqrt(0.114f * bgrafloat.z * bgrafloat.z + 0.587f * 
      bgrafloat.y * bgrafloat.y + 0.299 * bgrafloat.x * bgrafloat.x);
    // x y z
    // b g r
    right_direction += right_element * luminance;
    down_direction += down_element * luminance;
  }

  float gradient_magnitude = sqrt(right_direction * right_direction + down_direction * down_direction);

  uint4 output_bgra;
  output_bgra.x = output_bgra.y = output_bgra.z = (uint) (gradient_magnitude * 255.0f);
  output_bgra.w = 255;
  write_imageui(dstImg, base_coord, output_bgra);
}